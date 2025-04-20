using System.Numerics;
using Content.Shared.ADT.MesonVision;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Content.Shared.Light.Components;
using Content.Shared.StepTrigger.Components;

namespace Content.Client.ADT.MesonVision;

public sealed class MesonVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly SharedTransformSystem _xformSystem;
    private readonly EntityQuery<OccluderComponent> _replacement;
    private readonly EntityQuery<StepTriggerComponent> _steptrigger;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly List<MesonVisionRenderEntry> _entries = new(64);

    public MesonVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSystem = _entity.System<SharedTransformSystem>();
        _replacement = _entity.GetEntityQuery<OccluderComponent>();
        _steptrigger = _entity.GetEntityQuery<StepTriggerComponent>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out MesonVisionComponent? nightVision) ||
            nightVision.State != MesonVisionState.Full)
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
            if (!_replacement.HasComponent(entity) && !_steptrigger.HasComponent(entity)) continue;
            if (!_spriteQuery.TryGetComponent(entity, out var sprite)) continue;
            if (!_xformQuery.TryGetComponent(entity, out var xform)) continue;

            if (xform.MapID != mapId) continue;

            var worldPos = _xformSystem.GetWorldPosition(xform);
            if (!worldBounds.Contains(worldPos)) continue;

            _entries.Add(new MesonVisionRenderEntry(
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

public readonly record struct MesonVisionRenderEntry(
    (EntityUid, SpriteComponent, TransformComponent) Ent,
    MapId? Map,
    Angle EyeRot,
    int Priority,
    float? Transparency);
