using System.Numerics;
using Content.Client.Graphics;
using Content.Shared.ADT.Mirror;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.ADT.Mirror;

public sealed partial class MirrorOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StencilClearShader = "StencilClear";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private const LookupFlags ReflectionLookupFlags = LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries;
    private const float MaxReflectionAlpha = 0.9f;

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private SpriteSystem _sprite = default!;
    private TransformSystem _transform = default!;
    private EntityLookupSystem _lookup = default!;
    private MirrorSystem _mirror = default!;

    private readonly ShaderInstance _stencilMaskShader;
    private readonly ShaderInstance _stencilClearShader;
    private readonly ShaderInstance _reflectionShader;

    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private readonly HashSet<Entity<MirrorReflectionComponent>> _reflections = new();
    private readonly Dictionary<EntityUid, bool> _canBeSeenCache = new();
    private readonly Action _renderToTargetAction;

    private DrawingHandleWorld _worldHandle = default!;
    private Matrix3x2 _worldToTarget;
    private Matrix3x2 _worldToTargetLinear;
    private bool _targetFlipped;

    private Entity<SpriteComponent> _targetEntity;
    private Vector2 _targetPosition;
    private Angle _targetRotation;
    private Angle _targetEyeRotation;
    private Direction? _targetDirection;
    private bool _drawnAny;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public MirrorOverlay()
    {
        IoCManager.InjectDependencies(this);

        _stencilMaskShader = _prototypeManager.Index(StencilMaskShader).Instance();
        _stencilClearShader = _prototypeManager.Index(StencilClearShader).Instance();

        _reflectionShader = _prototypeManager.Index(UnshadedShader).InstanceUnique();
        _reflectionShader.Stencil = new StencilParameters
        {
            Enabled = true,
            Ref = 1,
            Op = StencilOp.Keep,
            Func = StencilFunc.Equal,
        };

        _renderToTargetAction = RenderToTarget;
        ZIndex = (int)DrawDepth.BelowMobs;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _sprite ??= _entityManager.System<SpriteSystem>();
        _transform ??= _entityManager.System<TransformSystem>();
        _lookup ??= _entityManager.System<EntityLookupSystem>();
        _mirror ??= _entityManager.System<MirrorSystem>();

        return base.BeforeDraw(args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye;
        if (eye == null)
            return;

        var mapId = args.MapId;
        var worldAabb = args.WorldAABB;
        CachedResources? res = null;

        _canBeSeenCache.Clear();
        _drawnAny = false;

        var mirrors = _entityManager.AllEntityQueryEnumerator<MirrorComponent, SpriteComponent, TransformComponent>();
        while (mirrors.MoveNext(out var uid, out var mirror, out var sprite, out var transform))
        {
            if (transform.MapID != mapId)
                continue;

            var (worldPosition, worldRotation) = _transform.GetWorldPositionRotation(transform);
            var bounds = _sprite.CalculateBounds((uid, sprite), worldPosition, worldRotation, eye.Rotation);
            if (!bounds.CalcBoundingBox().Intersects(worldAabb))
                continue;

            res ??= PrepareTarget(args);

            var mirrorPosition = _sprite.GetSpriteWorldPosition((uid, sprite, transform));
            var normalAngle = worldRotation + sprite.Rotation + mirror.DirRotation;
            var normal = normalAngle.ToVec().Normalized();
            var mirrorData = new MirrorData(
                (uid, sprite),
                worldPosition,
                worldRotation,
                bounds,
                mirror,
                mirrorPosition,
                normalAngle,
                normal,
                Vector2.Dot(eye.Position.Position - mirrorPosition, normal),
                (normalAngle + eye.Rotation + mirror.DirRotation).GetCardinalDir() is Direction.South);

            RenderEntities(args, eye, res, mirrorData);
        }

        if (_drawnAny)
            args.WorldHandle.UseShader(null);
    }

    private CachedResources PrepareTarget(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.Target?.Texture.Size != args.Viewport.Size)
        {
            res.Target?.Dispose();
            res.Target = _clyde.CreateRenderTarget(
                args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                name: "mirror-reflection");
        }

        _worldHandle = args.WorldHandle;
        _worldToTarget = args.Viewport.GetWorldToLocalMatrix();
        _worldToTargetLinear = _worldToTarget with
        {
            M31 = 0f,
            M32 = 0f,
        };
        _targetFlipped = _worldToTargetLinear.GetDeterminant() < 0f;

        return res;
    }

    private void RenderEntities(in OverlayDrawArgs args, IEye eye, CachedResources res, MirrorData mirrorData)
    {
        var mirror = mirrorData.Comp;

        _reflections.Clear();
        if (mirror.FadeFactor > 0)
            _lookup.GetEntitiesInRange(args.MapId, mirrorData.Position, mirror.GatherOffset + 1f / mirror.FadeFactor, _reflections, ReflectionLookupFlags);
        else
            _lookup.GetEntitiesIntersecting(args.MapId, args.WorldAABB, _reflections, ReflectionLookupFlags);

        foreach (var (uid, reflection) in _reflections)
        {
            if (!_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite)
                || !_entityManager.TryGetComponent<TransformComponent>(uid, out var transform))
                continue;

            var (sourcePosition, sourceRotation) = _transform.GetWorldPositionRotation(transform);
            if (!CanReflect(uid, reflection, sprite, sourcePosition, mirrorData, args.WorldAABB))
                continue;

            RenderReflection(args, (uid, sprite), sourcePosition, sourceRotation, eye, res.Target!, mirrorData);
        }
    }

    private void RenderReflection(in OverlayDrawArgs args, Entity<SpriteComponent> entity, Vector2 sourcePosition,
                                  Angle sourceRotation, IEye eye, IRenderTexture target, MirrorData mirrorData)
    {
        var sprite = entity.Comp;
        var mirror = mirrorData.Comp;
        var normal = mirrorData.Normal;

        var distance = (sourcePosition - mirrorData.Position).Length();
        var alpha = GetReflectionAlpha(sprite.Color.A, distance, mirror.ToleratedDistance, mirror.FadeFactor);
        if (alpha <= 0f)
            return;

        var offsetSourcePosition = sourcePosition + normal * mirror.GatherOffset;
        var reflectedPosition = offsetSourcePosition - 2f * Vector2.Dot(offsetSourcePosition - mirrorData.Position, normal) * normal;
        var drawPosition = reflectedPosition - normal * mirror.ReflectionOffset;

        Angle rotation;
        Direction? direction;
        Vector2 flipScale;

        if (mirrorData.ViewedFromSouth)
        {
            rotation = sourceRotation;
            direction = sprite.EnableDirectionOverride ? sprite.DirectionOverride : null;
            flipScale = new Vector2(1f, -1f);
        }
        else
        {
            rotation = mirrorData.NormalAngle * 2f - sourceRotation;
            direction = (rotation + eye.Rotation).GetCardinalDir() switch
            {
                Direction.West => Direction.East,
                Direction.East => Direction.West,
                var other => other,
            };
            flipScale = new Vector2(-1f, 1f);
        }

        var scale = sprite.Scale;
        var localMatrix = sprite.LocalMatrix;

        try
        {
            _sprite.SetScale(entity.AsNullable(), scale * flipScale);
            sprite.LocalMatrix = Matrix3x2.Multiply(sprite.LocalMatrix, _worldToTargetLinear);

            _targetEntity = entity;
            _targetPosition = Vector2.Transform(drawPosition, _worldToTarget);
            (_targetRotation, _targetEyeRotation) = GetTargetRotations(sprite, rotation, eye.Rotation);
            _targetDirection = direction;

            _worldHandle.RenderInRenderTarget(target, _renderToTargetAction, Color.Transparent);
        }
        finally
        {
            _sprite.SetScale(entity.AsNullable(), scale);
            sprite.LocalMatrix = localMatrix;
            _targetEntity = default;
        }

        _worldHandle.SetTransform(Matrix3x2.Identity);
        _worldHandle.UseShader(_stencilClearShader);
        _worldHandle.DrawRect(args.WorldAABB, Color.White);
        _drawnAny = true;

        _worldHandle.UseShader(_stencilMaskShader);
        _sprite.RenderSprite(mirrorData.Mirror, _worldHandle, eye.Rotation, mirrorData.WorldRotation, mirrorData.WorldPosition);

        _worldHandle.SetTransform(Matrix3x2.Identity);
        _worldHandle.UseShader(_reflectionShader);
        _worldHandle.DrawTextureRect(target.Texture, args.WorldBounds, Color.White.WithAlpha(alpha));

        _worldHandle.SetTransform(Matrix3x2.Identity);
        _worldHandle.UseShader(_stencilClearShader);
        _worldHandle.DrawRect(mirrorData.Bounds, Color.White);
    }

    private (Angle World, Angle Eye) GetTargetRotations(SpriteComponent sprite, Angle worldRotation, Angle eyeRotation)
    {
        if (!_targetFlipped)
            return (worldRotation, eyeRotation);

        if (sprite.NoRotation)
            return (worldRotation + eyeRotation * 2f, -eyeRotation);

        var cardinal = sprite.SnapCardinals
            ? (worldRotation + eyeRotation).Reduced().FlipPositive().RoundToCardinalAngle()
            : Angle.Zero;

        Angle world = cardinal * 2f - worldRotation;
        return (world, worldRotation + eyeRotation - world);
    }

    private void RenderToTarget()
    {
        _worldHandle.UseShader(null);
        _sprite.RenderSprite(_targetEntity, _worldHandle, _targetEyeRotation, _targetRotation, _targetPosition, _targetDirection);
    }

    private bool CanReflect(EntityUid uid, MirrorReflectionComponent reflection, SpriteComponent sprite,
                            Vector2 sourcePosition, MirrorData mirrorData, Box2 worldAabb)
    {
        if (!sprite.Visible)
            return false;

        if (!worldAabb.Contains(sourcePosition))
            return false;

        var mirror = mirrorData.Comp;
        var offset = sourcePosition - mirrorData.Position;

        if (Vector2.Dot(offset, mirrorData.Normal) * mirrorData.ViewerSide <= 0f)
            return false;

        if (mirror.FadeFactor > 0)
        {
            var maxDistance = mirror.GatherOffset + 1f / mirror.FadeFactor;
            if (offset.LengthSquared() >= maxDistance * maxDistance)
                return false;
        }

        if (_entityManager.HasComponent<MirrorComponent>(uid))
            return false;

        if (!reflection.ReflectIfInvisible
            && _entityManager.TryGetComponent<StealthComponent>(uid, out var stealth)
            && stealth.Enabled)
            return false;

        if (!_canBeSeenCache.TryGetValue(uid, out var canBeSeen))
        {
            canBeSeen = _mirror.CanBeSeenInMirrors(uid);
            _canBeSeenCache[uid] = canBeSeen;
        }

        return canBeSeen;
    }

    private static float GetReflectionAlpha(float originalAlpha, float distance, float toleratedDistance, float fadeFactorMod)
    {
        var fadeFactor = MathF.Max(distance - toleratedDistance, 0f);
        return Math.Clamp(originalAlpha - fadeFactor * fadeFactorMod, 0f, MaxReflectionAlpha);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();
        _reflectionShader.Dispose();

        base.DisposeBehavior();
    }

    private readonly record struct MirrorData(
        Entity<SpriteComponent> Mirror,
        Vector2 WorldPosition,
        Angle WorldRotation,
        Box2Rotated Bounds,
        MirrorComponent Comp,
        Vector2 Position,
        Angle NormalAngle,
        Vector2 Normal,
        float ViewerSide,
        bool ViewedFromSouth);

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? Target;

        public void Dispose()
        {
            Target?.Dispose();
        }
    }
}
