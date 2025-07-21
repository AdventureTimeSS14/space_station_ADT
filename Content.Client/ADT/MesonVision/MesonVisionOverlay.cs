using System.Numerics;
using Content.Shared.ADT.MesonVision;
using Content.Shared.Doors.Components;
using Content.Shared.Light.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.ADT.MesonVision;

public sealed class MesonVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly SharedTransformSystem _xformSystem;
    private readonly ContainerSystem _container;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<TagComponent> _tagQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly List<MesonVisionRenderEntry> _entries = new(64);

    public MesonVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _container = _entity.System<ContainerSystem>();
        _xformSystem = _entity.System<SharedTransformSystem>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
        _tagQuery = _entity.GetEntityQuery<TagComponent>();
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

        foreach (EntityUid entity in _entity.GetEntities())
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
                sprite.DrawDepth,
                1f));
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

        Vector2 position = _xformSystem.GetWorldPosition(xform);
        Angle rotation = _xformSystem.GetWorldRotation(xform);

        handle.SetTransform(position, rotation);

        var originalColor = sprite.Color;

        sprite.Color = originalColor.WithAlpha(alpha);
        sprite.Render(handle, eyeRot, rotation, position: position);
        sprite.Color = originalColor;
        handle.SetTransform(Vector2.Zero, Angle.Zero);
    }
}

public readonly record struct MesonVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    Angle EyeRot,
    int Priority,
    float? Transparency);