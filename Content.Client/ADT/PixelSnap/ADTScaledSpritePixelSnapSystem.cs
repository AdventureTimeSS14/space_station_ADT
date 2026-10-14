using System.Numerics;
using Content.Client.ADT.Posing;
using Content.Client.Eye;
using Content.Client.Orbit;
using Content.Client.Viewport;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.ADT.PixelSnap;

public sealed class ADTScaledSpritePixelSnapSystem : EntitySystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;

    private Dictionary<EntityUid, SnapState> _states = new();
    private Dictionary<EntityUid, SnapState> _nextStates = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(Robust.Client.Physics.PhysicsSystem));
        UpdatesAfter.Add(typeof(Robust.Client.GameObjects.EyeSystem));
        UpdatesAfter.Add(typeof(EyeLerpingSystem));
        UpdatesAfter.Add(typeof(PosingSystem));
        UpdatesAfter.Add(typeof(OrbitVisualsSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _nextStates.Clear();

        if (_eyeManager.MainViewport is not ScalingViewport viewport)
        {
            RestoreAll();
            return;
        }

        var eye = _eyeManager.CurrentEye;
        if (eye.Position.MapId == MapId.Nullspace)
        {
            RestoreAll();
            return;
        }

        var renderScale = viewport.CurrentRenderScale;
        var viewportSize = (Vector2) (viewport.ViewportSize * renderScale);
        var viewScale = eye.Scale * renderScale * new Vector2(EyeManager.PixelsPerMeter, -EyeManager.PixelsPerMeter);
        var viewPosition = eye.Position.Position + eye.Offset;
        var viewRotation = eye.Rotation;

        if (!IsFinite(viewScale) || viewScale.X == 0f || viewScale.Y == 0f)
        {
            RestoreAll();
            return;
        }

        var query = EntityQueryEnumerator<InputMoverComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            var hadState = _states.Remove(uid, out var state);
            var baseOffset = hadState && sprite.Offset == state.Expected
                ? sprite.Offset - state.Applied
                : sprite.Offset;

            if (sprite.Scale == Vector2.One || xform.MapID != eye.Position.MapId)
            {
                if (hadState && sprite.Offset == state.Expected)
                    _sprite.SetOffset((uid, sprite), baseOffset);

                continue;
            }

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);

            if (xform.GridUid is { } grid && TryComp(grid, out TransformComponent? gridXform))
                worldPos += GetPixelSnapOffset(gridXform.LocalPosition, viewPosition, viewRotation, viewScale, viewportSize);

            var offsetWorld = sprite.NoRotation
                ? (-viewRotation).RotateVec(baseOffset)
                : worldRot.RotateVec(baseOffset);

            var snap = GetPixelSnapOffset(worldPos + offsetWorld, viewPosition, viewRotation, viewScale, viewportSize);
            var applied = sprite.NoRotation
                ? viewRotation.RotateVec(snap)
                : (-worldRot).RotateVec(snap);

            if (!IsFinite(applied) || !IsFinite(baseOffset))
            {
                if (hadState && sprite.Offset == state.Expected)
                    _sprite.SetOffset((uid, sprite), baseOffset);

                continue;
            }

            var expected = baseOffset + applied;
            if (sprite.Offset != expected)
                _sprite.SetOffset((uid, sprite), expected);

            _nextStates[uid] = new SnapState(applied, expected);
        }

        RestoreAll();
        (_states, _nextStates) = (_nextStates, _states);
    }

    private void RestoreAll()
    {
        foreach (var (uid, state) in _states)
        {
            if (TryComp(uid, out SpriteComponent? sprite) && sprite.Offset == state.Expected)
                _sprite.SetOffset((uid, sprite), sprite.Offset - state.Applied);
        }

        _states.Clear();
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
    }

    private static Vector2 GetPixelSnapOffset(
        Vector2 worldPosition,
        Vector2 viewPosition,
        Angle viewRotation,
        Vector2 viewScale,
        Vector2 viewportSize)
    {
        var viewPositionRelative = viewRotation.RotateVec(worldPosition - viewPosition);
        var screenPosition = viewPositionRelative * viewScale + viewportSize / 2f;
        var screenOffset = screenPosition.Rounded() - screenPosition;
        var viewOffset = screenOffset / viewScale;
        return (-viewRotation).RotateVec(viewOffset);
    }

    private readonly record struct SnapState(Vector2 Applied, Vector2 Expected);
}
