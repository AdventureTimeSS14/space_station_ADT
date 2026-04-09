using System.Numerics;
using Content.Shared.ADT.Abilities;
using Content.Shared.ADT.Trail;
using Content.Shared.DrawDepth;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.ADT.Trail;
public sealed class SandevistanTrailOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly IEntityManager _entManager;
    private readonly ISharedPlayerManager _playerManager;
    private readonly IGameTiming _timing;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private OccluderSystem _occluderSystem = default!;
    private const float OcclusionCheckRange = 100f;

    public SandevistanTrailOverlay(IEntityManager entManager, ISharedPlayerManager playerManager, IGameTiming timing)
    {
        ZIndex = (int)DrawDepth.Effects + 25;

        _entManager = entManager;
        _playerManager = playerManager;
        _timing = timing;
        _sprite = _entManager.System<SpriteSystem>();
        _transform = _entManager.System<TransformSystem>();
        _occluderSystem = _entManager.System<OccluderSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye;
        if (eye == null)
            return;

        var localPlayer = _playerManager.LocalEntity;
        if (!localPlayer.HasValue || !localPlayer.Value.Valid)
            return;

        var eyeRot = eye.Rotation;
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();

        handle.UseShader(null);

        var cameraMapPos = eye.Position;
        var cameraPos = cameraMapPos.Position;

        var query = _entManager.EntityQueryEnumerator<TrailComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trail, out var xform))
        {
            if (uid != localPlayer.Value)
                continue;

            if (trail.TrailData.Count == 0)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform, xformQuery);

            handle.UseShader(null);

            if (trail.RenderedEntity != null)
            {
                Direction? direction = null;
                var rot = rotation;
                if (trail.RenderedEntityRotationStrategy == RenderedEntityRotationStrategy.Trail)
                {
                    var dirRot = rotation + eyeRot;
                    direction = dirRot.GetCardinalDir();
                }
                else if (trail.RenderedEntityRotationStrategy == RenderedEntityRotationStrategy.RenderedEntity)
                    rot = _transform.GetWorldRotation(trail.RenderedEntity.Value);

                if (spriteQuery.TryComp(trail.RenderedEntity.Value, out var sprite))
                {
                    var ownerPos = _transform.GetWorldPosition(trail.RenderedEntity.Value);
                    const float ownerOverlapRadius = 0.4f;

                    handle.SetTransform(Matrix3x2.Identity);
                    var trailColor = trail.Color;

                    foreach (var data in trail.TrailData)
                    {
                        var scale = trail.Scale != 1f ? trail.Scale : data.Scale;

                        if (trailColor.A <= 0.01f || scale <= 0.01f || data.MapId != args.MapId)
                            continue;

                        var worldPosition = data.Position;
                        if (!bounds.Contains(worldPosition))
                            continue;

                        var distToOwner = Vector2.Distance(worldPosition, ownerPos);
                        if (distToOwner < ownerOverlapRadius)
                            continue;

                        if (IsOccluded(cameraPos, worldPosition, data.MapId))
                            continue;

                        if (trail.RenderedEntityRotationStrategy == RenderedEntityRotationStrategy.Particle)
                        {
                            rot = data.Angle;
                            direction = (rot + eyeRot).GetCardinalDir();
                        }

                        var originalColor = sprite.Color;
                        var originalScale = sprite.Scale;
                        sprite.Color = trailColor;
                        sprite.Scale *= scale;
                        sprite.Render(handle, eyeRot, rot, direction, worldPosition);
                        sprite.Color = originalColor;
                        sprite.Scale = originalScale;
                    }
                }
                handle.UseShader(null);
                continue;
            }

            if (trail.Sprite == null)
            {
                handle.SetTransform(Matrix3x2.Identity);
                var trailColor = trail.Color;

                if (xform.MapID == args.MapId)
                {
                    var start = trail.TrailData[^1].Position;
                    var scale = trail.Scale != 1f ? trail.Scale : trail.TrailData[^1].Scale;
                    if (!IsOccluded(cameraPos, start, xform.MapID))
                        DrawTrailLine(start, position, trailColor, scale, bounds, handle);
                }

                for (var i = 1; i < trail.TrailData.Count; i++)
                {
                    var data = trail.TrailData[i];
                    var prevData = trail.TrailData[i - 1];

                    if (data.MapId == args.MapId && prevData.MapId == args.MapId)
                    {
                        var midpoint = (data.Position + prevData.Position) * 0.5f;
                        var scale = trail.Scale != 1f ? trail.Scale : data.Scale;
                        if (!IsOccluded(cameraPos, midpoint, data.MapId))
                            DrawTrailLine(prevData.Position, data.Position, trailColor, scale, bounds, handle);
                    }
                }
                handle.UseShader(null);
                continue;
            }

            var textureSize = _sprite.Frame0(trail.Sprite).Size;
            var pos = -(Vector2)textureSize / 2f / EyeManager.PixelsPerMeter;
            var trailTextureColor = trail.Color;

            foreach (var data in trail.TrailData)
            {
                var scale = trail.Scale != 1f ? trail.Scale : data.Scale;

                if (trailTextureColor.A <= 0.01f || scale <= 0.01f || data.MapId != args.MapId)
                    continue;

                var worldPosition = data.Position;
                if (!bounds.Contains(worldPosition))
                    continue;

                if (IsOccluded(cameraPos, worldPosition, data.MapId))
                    continue;

                var scaleMatrix = Matrix3x2.CreateScale(new Vector2(scale, scale));
                var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);

                var time = _timing.CurTime > data.SpawnTime ? _timing.CurTime - data.SpawnTime : TimeSpan.Zero;
                var texture = _sprite.GetFrame(trail.Sprite, time);

                handle.SetTransform(Matrix3x2.Multiply(scaleMatrix, worldMatrix));
                handle.DrawTexture(texture, pos, data.Angle, trailTextureColor);
            }
            handle.UseShader(null);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTrailLine(Vector2 start,
        Vector2 end,
        Color color,
        float scale,
        Box2 bounds,
        DrawingHandleWorld handle)
    {
        if (color.A <= 0.01f || scale <= 0.01f)
            return;

        if (!bounds.Contains(start) || !bounds.Contains(end))
            return;

        var halfScale = scale * 0.5f;
        var direction = end - start;
        var angle = direction.ToAngle();
        var box = new Box2(start - new Vector2(0f, halfScale),
            start + new Vector2(direction.Length(), halfScale));
        var boxRotated = new Box2Rotated(box, angle, start);
        handle.DrawRect(boxRotated, color);
    }

    private bool IsOccluded(Vector2 cameraPos, Vector2 targetPos, MapId mapId)
    {
        if (mapId == MapId.Nullspace)
            return false;

        var distance = Vector2.Distance(cameraPos, targetPos);
        var checkRange = distance > OcclusionCheckRange ? distance : OcclusionCheckRange;

        var origin = new MapCoordinates(cameraPos, mapId);
        var target = new MapCoordinates(targetPos, mapId);

        return !_occluderSystem.InRangeUnoccluded(origin, target, checkRange, ignoreTouching: true);
    }
}
