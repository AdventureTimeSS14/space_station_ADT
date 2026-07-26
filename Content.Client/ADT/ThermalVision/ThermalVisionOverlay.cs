using System.Numerics;
using Content.Shared.ADT.ThermalVision;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Content.Shared.Mobs;

namespace Content.Client.ADT.ThermalVision;

public sealed class ThermalVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly SharedTransformSystem _xformSystem;
    private readonly ContainerSystem _container;
    private readonly EntityQuery<MobStateComponent> _mobQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly List<ThermalVisionRenderEntry> _entries = new(64);

    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _container = _entity.System<ContainerSystem>();
        _xformSystem = _entity.System<SharedTransformSystem>();
        _mobQuery = _entity.GetEntityQuery<MobStateComponent>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out ThermalVisionComponent? nightVision) ||
            nightVision.State != ThermalVisionState.Full)
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
            if (!_mobQuery.TryGetComponent(entity, out var mob) || mob.CurrentState == MobState.Dead) continue;
            if (!_spriteQuery.TryGetComponent(entity, out var sprite)) continue;
            if (!_xformQuery.TryGetComponent(entity, out var xform)) continue;
            if (_container.TryGetOuterContainer(entity, xform, out var container)) continue;
            if (xform.MapID != mapId) continue;

            var worldPos = _xformSystem.GetWorldPosition(xform);
            if (!worldBounds.Contains(worldPos)) continue;

            _entries.Add(new ThermalVisionRenderEntry(
                (entity, sprite, xform),
                mapId,
                eyeRot,
                sprite.DrawDepth,
                0.5f
            ));
        }

        foreach (var entry in _entries)
        {
            RenderFast(entry.Ent, worldHandle, entry.EyeRot, entry.Transparency ?? 0.5f, nightVision.Color);
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

public readonly record struct ThermalVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    Angle EyeRot,
    int Priority,
    float? Transparency);
