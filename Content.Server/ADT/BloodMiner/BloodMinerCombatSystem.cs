using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.ADT.BloodMiner;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BloodMiner;

public sealed class BloodMinerCombatSystem : EntitySystem
{
    [Dependency] private readonly BloodMinerDashSystem _dash = default!;
    [Dependency] private readonly BloodMinerSystem _miner = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float MinShootDistance = 1.5f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodMinerComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (HasComp<ActorComponent>(uid))
                continue;

            if (!_npc.IsAwake(uid, htn))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (now < comp.NextDecisionAt)
                continue;

            comp.NextDecisionAt = now + comp.DecisionInterval;

            if (!htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager)
                || TerminatingOrDeleted(target))
                continue;

            if (!TargetOnSameGrid(uid, target))
            {
                htn.Blackboard.Remove<EntityUid>("Target");
                continue;
            }

            OpenFire((uid, comp), target);
        }
    }

    private bool TargetOnSameGrid(EntityUid ent, EntityUid target)
    {
        return _transform.GetGrid(ent) == _transform.GetGrid(target);
    }

    private void OpenFire(Entity<BloodMinerComponent> ent, EntityUid target)
    {
        var targetCoords = _transform.GetMapCoordinates(target);
        if (targetCoords.MapId == MapId.Nullspace)
            return;

        var origin = _transform.GetMapCoordinates(ent);
        if (origin.MapId != targetCoords.MapId)
            return;

        if ((targetCoords.Position - origin.Position).Length() > ent.Comp.DashRange)
            _dash.TryDash(ent, targetCoords);

        TryShoot(ent, target);
        _miner.TryTransformSaw(ent);
    }

    private bool TryShoot(Entity<BloodMinerComponent> ent, EntityUid target)
    {
        if (_timing.CurTime < ent.Comp.NextShotAt)
            return false;

        if (_mobState.IsDead(target))
            return false;

        var origin = _transform.GetMapCoordinates(ent);
        var targetCoords = _transform.GetMapCoordinates(target);
        if (origin.MapId != targetCoords.MapId)
            return false;

        var distance = (targetCoords.Position - origin.Position).Length();
        if (distance > ent.Comp.DashRange || distance < MinShootDistance)
            return false;

        var gun = Spawn(ent.Comp.GunProto, Transform(ent).Coordinates);
        if (!TryComp<GunComponent>(gun, out var gunComp))
        {
            QueueDel(gun);
            return false;
        }

        var shot = _gun.AttemptShoot(ent.Owner, (gun, gunComp), Transform(target).Coordinates, target);
        QueueDel(gun);

        if (shot)
            ent.Comp.NextShotAt = _timing.CurTime + ent.Comp.RangedCooldown;

        return shot;
    }
}
