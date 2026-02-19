using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;

namespace Content.Shared.ADT.SpaceWhale;

public abstract class SharedTailedEntitySystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected void UpdateTailPositions(Entity<TailedEntityComponent, TransformComponent> ent, float frameTime)
    {
        var (uid, comp, xform) = ent;

        // Use the head's world position to determine whether anything moved.
        var headPos = _transformSystem.GetWorldPosition(xform);
        if (headPos == comp.LastPos)
            return;

        for (var i = 0; i < comp.TailSegments.Count; i++)
        {
            if (!TryGetEntity(comp.TailSegments[i], out var segment))
                continue;

            EntityUid? next = null;

            if (i <= 0)
                next = uid;
            else
                TryGetEntity(comp.TailSegments[i - 1], out next);

            if (!next.HasValue)
                continue;

            var segPos = _transformSystem.GetWorldPosition(segment.Value);
            var nextPos = _transformSystem.GetWorldPosition(next.Value);
            var nextRot = Angle.FromWorldVec(_transformSystem.GetWorldPosition(next.Value) - segPos);

            // Compute the desired position: keep `Spacing` units behind the next entity along the line
            // from the segment to the next entity. If the segment is exactly on top of the target, fall back
            // to using the target's forward vector.
            var toTarget = nextPos - segPos;
            var distance = toTarget.Length();

            Vector2 desiredPos = Vector2.Zero;
            if (distance > 0.0001f)
            {
                var dir = toTarget / distance;
                desiredPos = nextPos - dir * comp.Spacing;
            }
            else
            {
                desiredPos = nextPos - nextRot.ToWorldVec() * comp.Spacing;
            }

            // Server remains authoritative and snaps to maintain exact spacing.
            _transformSystem.SetWorldPositionRotation(segment.Value, desiredPos, nextRot);
        }

        comp.LastPos = headPos;
    }
}
