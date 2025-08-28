using System.Numerics;
using Content.Shared.Traits.Assorted;
using Content.Shared.ADT.MesonVision;
using Content.Shared.Doors.Components;
using Content.Shared.Light.Components;
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

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly List<MesonVisionRenderEntry> _entries = new(64);

    public MesonVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _container = _entity.System<ContainerSystem>();
        _xformSystem = _entity.System<SharedTransformSystem>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out MesonVisionComponent? nightVision) ||
            nightVision.State != MesonVisionState.Full ||
            _entity.HasComponent<PermanentBlindnessComponent>(_player.LocalEntity))
        {
            return;
        }

        var eye = args.Viewport.Eye;
        if (eye == null) return;

        var mapId = eye.Position.MapId;
        var eyeRot = eye.Rotation;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;

        foreach (var entity in _entity.GetEntities())
        {
            // Ganimed edit start
            if (!_spriteQuery.TryGetComponent(entity, out var sprite) ||
                !_xformQuery.TryGetComponent(entity, out var xform) ||
                xform.MapID != mapId ||
                _container.TryGetOuterContainer(entity, xform, out var _))
            {
                continue;
            }
            // Ganimed edit end

            var worldPos = _xformSystem.GetWorldPosition(xform);
            if (!worldBounds.Contains(worldPos)) continue;

            // Ganimed edit start
            var isWall = _entity.HasComponent<SunShadowCastComponent>(entity) || _entity.HasComponent<IsRoofComponent>(entity);
            var isDoor = _entity.HasComponent<DoorComponent>(entity);

            if (!isWall && !isDoor)
                continue;
            // Ganimed edit end 

            _entries.Add(new MesonVisionRenderEntry(
                (entity, sprite, xform),
                mapId,
                eyeRot,
                sprite.DrawDepth,
                1f
            ));
        }

        foreach (var entry in _entries)
        {
            RenderFast(entry.Ent, worldHandle, entry.EyeRot, entry.Transparency ?? 1f, nightVision.Color);
        }

        _entries.Clear();
    }

    private void RenderFast(
        Entity<SpriteComponent, TransformComponent> ent,
        DrawingHandleWorld handle,
        Angle eyeRot,
        float alpha,
        Color color)
    {
        var (uid, sprite, xform) = ent;
        var position = _xformSystem.GetWorldPosition(xform);
        var rotation = _xformSystem.GetWorldRotation(xform);

        handle.SetTransform(position, rotation);

        var originalColor = sprite.Color;
        sprite.Color = color.WithAlpha(alpha);
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
