using System.Numerics;
using Content.Shared.ADT.Mirror;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Content.Shared.Stealth.Components;

namespace Content.Client.ADT.Mirror;

public sealed partial class MirrorOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StencilClearShader = "StencilClear";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilEqualDrawShader = "StencilEqualDraw";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEyeManager _eyeMan = default!;
    private SpriteSystem _sprite = default!;
    private TransformSystem _transform = default!;
    private ContainerSystem _container = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public MirrorOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = (int)DrawDepth.BelowMobs;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _sprite ??= _entityManager.System<SpriteSystem>();
        _transform ??= _entityManager.System<TransformSystem>();
        _container ??= _entityManager.System<ContainerSystem>();

        return base.BeforeDraw(args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye;
        if (eye == null)
            return;

        var mapId = args.MapId;
        var worldAabb = args.WorldAABB;

        var mirrors = _entityManager.AllEntityQueryEnumerator<MirrorComponent, SpriteComponent, TransformComponent>();
        var mirrorData = new List<(MirrorComponent Component, Vector2 Position, Angle Rotation)>();
        while (mirrors.MoveNext(out var uid, out var component, out var sprite, out var transform))
        {
            if (transform.MapID == mapId)
            {
                var position = _sprite.GetSpriteWorldPosition((uid, sprite, transform));
                var rotation = _transform.GetWorldRotation(transform) + sprite.Rotation;
                mirrorData.Add((component, position, rotation));
            }
        }

        if (mirrorData.Count == 0)
            return;

        var worldHandle = args.WorldHandle;

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_prototypeManager.Index(StencilClearShader).Instance());
        worldHandle.DrawRect(worldAabb, Color.White);

        // Сама отрисовка начинается тут
        // Каждое зеркало делает свою маску и рисует сущности, которые может
        var mirrorEntities = _entityManager.AllEntityQueryEnumerator<MirrorComponent, SpriteComponent, TransformComponent>();
        while (mirrorEntities.MoveNext(out var uid, out var mirror, out var sprite, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            worldHandle.UseShader(_prototypeManager.Index(StencilMaskShader).Instance());

            _sprite.RenderSprite((uid, sprite), worldHandle, eye.Rotation, _transform.GetWorldRotation(transform),
                _transform.GetWorldPosition(transform));

            worldHandle.UseShader(_prototypeManager.Index(StencilEqualDrawShader).Instance());
            RenderEntities(worldAabb, eye, worldHandle, mapId,
                (mirror, _sprite.GetSpriteWorldPosition((uid, sprite, transform)),
                    _transform.GetWorldRotation(transform) + sprite.Rotation));

            worldHandle.UseShader(_prototypeManager.Index(StencilClearShader).Instance());
            worldHandle.SetTransform(Matrix3x2.Identity);
            worldHandle.DrawRect(worldAabb, Color.White);
        }

        worldHandle.UseShader(null);
    }

    private void RenderEntities(Box2 worldAabb, IEye eye, DrawingHandleWorld worldHandle, MapId mapId,
                                (MirrorComponent Component, Vector2 Position, Angle Rotation) mirrorData)
    {
        var entities = _entityManager.AllEntityQueryEnumerator<MirrorReflectionComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out var reflection, out var sprite, out var transform))
        {
            var (mirror, mirrorPosition, mirrorRotation) = mirrorData;
            var sourcePosition = _transform.GetWorldPosition(transform);
            var normalAngle = mirrorRotation + Angle.FromDegrees(mirror.DirRotation);
            var normal = normalAngle.ToVec().Normalized();

            if (!CanReflect(uid, reflection, transform, mirrorData, normal, mapId, eye, worldAabb))
                continue;

            var color = sprite.Color;
            var newColor = GetTransparentColor(uid, color, mirrorPosition, mirror.ToleratedDistance, mirror.FadeFactor);
            _sprite.SetColor(uid, newColor);

            // Этот ебучий слой ломал вообще всё
            // Не убирайте этот фикс
            var hiddenStencilLayers = new List<(ISpriteLayer Layer, bool Visible)>();
            if (_sprite.LayerMapTryGet((uid, sprite), HumanoidVisualLayers.StencilMask, out var stencilMaskLayer, false))
            {
                var stencilMask = sprite[stencilMaskLayer];
                hiddenStencilLayers.Add((stencilMask, stencilMask.Visible));
                stencilMask.Visible = false;

                if (stencilMaskLayer > 0)
                {
                    var stencilClear = sprite[stencilMaskLayer - 1];
                    hiddenStencilLayers.Add((stencilClear, stencilClear.Visible));
                    stencilClear.Visible = false;
                }
            }

            var offsetSourcePosition = sourcePosition + normal * mirror.GatherOffset;
            var reflectedPosition = offsetSourcePosition - 2f * Vector2.Dot(offsetSourcePosition - mirrorPosition, normal) * normal;

            var scale = sprite.Scale;

            if ((normalAngle + eye.Rotation + mirror.DirRotation).GetCardinalDir() is not Direction.South)
            {
                var reflectedFacing = normalAngle * 2f - _transform.GetWorldRotation(transform);
                var dir = (reflectedFacing + eye.Rotation).GetCardinalDir();

                switch (dir)
                {
                    case Direction.West:
                        dir = Direction.East;
                        break;
                    case Direction.East:
                        dir = Direction.West;
                        break;
                    default:
                        break;
                }

                _sprite.SetScale(uid, new Vector2(-scale.X, scale.Y));

                _sprite.RenderSprite((uid, sprite), worldHandle, eye.Rotation, reflectedFacing,
                    reflectedPosition - normal * mirror.ReflectionOffset, dir);

                _sprite.SetScale(uid, scale);
            }
            else
            {
                _sprite.SetScale(uid, new Vector2(scale.X, -scale.Y));

                _sprite.RenderSprite((uid, sprite), worldHandle, eye.Rotation, _transform.GetWorldRotation(transform),
                    reflectedPosition - normal * mirror.ReflectionOffset);

                _sprite.SetScale(uid, scale);
            }

            foreach (var (layer, visible) in hiddenStencilLayers)
                layer.Visible = visible;

            worldHandle.UseShader(_prototypeManager.Index(StencilEqualDrawShader).Instance());
            _sprite.SetColor(uid, color);
        }
    }

    private bool CanReflect(EntityUid uid, MirrorReflectionComponent reflection, TransformComponent transform,
                            (MirrorComponent, Vector2, Angle) mirrorData, Vector2 normal,
                            MapId mapId, IEye eye, Box2 worldAabb)
    {
        if (_entityManager.HasComponent<MirrorComponent>(uid) || transform.MapID != mapId)
            return false;

        if (!reflection.Active)
            return false;

        if (!reflection.ReflectIfInvisible && _entityManager.TryGetComponent<StealthComponent>(uid, out var stealth) && stealth.Enabled)
            return false;

        var sourcePosition = _transform.GetWorldPosition(transform);
        if (!worldAabb.Contains(sourcePosition) || _container.IsEntityInContainer(uid))
            return false;

        var (mirror, mirrorPosition, _) = mirrorData;

        var entitySide = Vector2.Dot(sourcePosition - mirrorPosition, normal);
        var viewerSide = Vector2.Dot(eye.Position.Position - mirrorPosition, normal);
        if (entitySide * viewerSide <= 0f)
            return false;

        if (mirror.FadeFactor > 0 && Vector2.Distance(sourcePosition, mirrorPosition) >= mirror.GatherOffset + 1f / mirror.FadeFactor)
            return false;

        return true;
    }

    private Color GetTransparentColor(EntityUid uid, Color originalColor, Vector2 mirrorPos, float toleratedDistance, float fadeFactorMod)
    {
        var dist = (_transform.GetWorldPosition(uid) - mirrorPos).Length();

        var fadeFactor = MathF.Max(dist - toleratedDistance, 0f);
        return originalColor.WithAlpha(Math.Clamp(originalColor.A - fadeFactor * fadeFactorMod, 0f, 0.9f));
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
    }
}
