using System.Numerics;
using Content.Shared.ADT.MesonVision;
using Content.Shared.Doors.Components;
using Content.Shared.Light.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.ADT.MesonVision;

public sealed class MesonVisionOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    private readonly SharedTransformSystem _xformSystem;
    private readonly ContainerSystem _container;
    private readonly SpriteSystem _spriteSystem;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<TagComponent> _tagQuery;
    private readonly EntityLookupSystem _lookup;
    private readonly HashSet<EntityUid> _visible = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly List<MesonVisionRenderEntry> _entries = new(64);

    private const int MesonDrawDepth = 0;

    public MesonVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _container = _entity.System<ContainerSystem>();
        _xformSystem = _entity.System<SharedTransformSystem>();
        _spriteSystem = _entity.System<SpriteSystem>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
        _tagQuery = _entity.GetEntityQuery<TagComponent>();
        _lookup = _entity.System<EntityLookupSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out MesonVisionComponent? nightVision) ||
            nightVision.State != MesonVisionState.Full)
            return;

        var eye = args.Viewport.Eye;
        if (eye == null)
            return;

        MapId mapId = eye.Position.MapId;
        Angle eyeRot = eye.Rotation;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;

        _visible.Clear();
        _lookup.GetEntitiesIntersecting(mapId, args.WorldAABB, _visible, LookupFlags.Static | LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate);

        foreach (var entity in _visible)
        {
            if (!_spriteQuery.TryGetComponent(entity, out var sprite) ||
                !_xformQuery.TryGetComponent(entity, out var xform) ||
                xform.MapID != mapId ||
                _container.TryGetOuterContainer(entity, xform, out _))
                continue;

            Vector2 worldPos = _xformSystem.GetWorldPosition(xform);
            if (!worldBounds.Contains(worldPos))
                continue;

            bool isWall = _entity.HasComponent<SunShadowCastComponent>(entity) || _entity.HasComponent<IsRoofComponent>(entity);
            bool isDoor = _entity.HasComponent<DoorComponent>(entity);

            bool hasMesonTag = false;
            if (_tagQuery.TryGetComponent(entity, out var tagComp))
            {
                foreach (var tag in tagComp.Tags)
                {
                    if (tag == "MesonVisible")
                    {
                        hasMesonTag = true;
                        break;
                    }
                }
            }

            if (!(isWall || isDoor || hasMesonTag))
                continue;

            _entries.Add(new MesonVisionRenderEntry(
                (entity, sprite, xform),
                mapId,
                eyeRot,
                MesonDrawDepth,
                nightVision.WallAlpha));
        }

        foreach (var entry in _entries)
        {
            RenderFastRaw(entry.Ent, worldHandle, entry.EyeRot, entry.Transparency ?? 1f);
        }

        _entries.Clear();
    }

    private void RenderFastRaw(
        (EntityUid uid, SpriteComponent sprite, TransformComponent xform) ent,
        DrawingHandleWorld handle,
        Angle eyeRot,
        float alpha)
    {
        var (uid, sprite, xform) = ent;

        var position = _xformSystem.GetWorldPosition(xform);
        var rotation = _xformSystem.GetWorldRotation(xform);

        handle.SetTransform(position, rotation);

        var originalDepth = sprite.DrawDepth;
        sprite.DrawDepth = MesonDrawDepth;

        var originalColor = sprite.Color;
        sprite.Color = originalColor.WithAlpha(alpha);

        var oreLayer = GetOreLayer(uid, sprite);
        var oreColor = oreLayer?.Color ?? Color.White;

        if (oreLayer != null)
            oreLayer.Color = oreColor.WithAlpha(0f);

        sprite.Render(handle, eyeRot, rotation, position: position);

        if (oreLayer != null)
            oreLayer.Color = oreColor;

        sprite.Color = originalColor;
        sprite.DrawDepth = originalDepth;

        handle.SetTransform(Vector2.Zero, Angle.Zero);
    }

    private ISpriteLayer? GetOreLayer(EntityUid uid, SpriteComponent sprite)
    {
        if (!_spriteSystem.LayerMapTryGet((uid, sprite), MiningScannerVisualLayers.Overlay, out var index, false))
            return null;

        return sprite[index];
    }
}

public readonly record struct MesonVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    Angle EyeRot,
    int Priority,
    float? Transparency);