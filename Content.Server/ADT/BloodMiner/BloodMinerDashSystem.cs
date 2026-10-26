using Content.Shared.ADT.BloodMiner;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BloodMiner;

public sealed class BloodMinerDashSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public bool TryDash(Entity<BloodMinerComponent> ent, MapCoordinates target)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextDashAt)
            return false;

        if (_mobState.IsDead(ent))
            return false;

        var origin = _transform.GetMapCoordinates(ent);
        if (origin.MapId == MapId.Nullspace || origin.MapId != target.MapId)
            return false;

        var diff = target.Position - origin.Position;
        var distance = diff.Length();
        if (distance < 0.01f)
            return false;

        var direction = diff / distance;

        var dashDistance = Math.Min(ent.Comp.DashRange, distance - 1f);
        if (dashDistance < 1f)
            return false;

        var ray = new CollisionRay(origin.Position, direction, (int)CollisionGroup.Impassable);
        foreach (var result in _physics.IntersectRay(origin.MapId, ray, dashDistance, ent.Owner, false))
        {
            dashDistance = Math.Min(dashDistance, result.Distance - 0.5f);
        }

        if (dashDistance < 1f)
            return false;

        if (ent.Comp.DashSmokeProto is { } smoke)
            Spawn(smoke, origin);

        _audio.PlayPvs(ent.Comp.DashSound, ent);
        _throwing.TryThrow(
            ent.Owner,
            direction * dashDistance,
            ent.Comp.DashSpeed,
            animated: false,
            playSound: false,
            doSpin: false,
            compensateFriction: true);

        ent.Comp.NextDashAt = now + ent.Comp.DashCooldown;
        Dirty(ent);
        return true;
    }
}
