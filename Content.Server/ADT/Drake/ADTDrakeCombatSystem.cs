using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Drake;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeCombatSystem : EntitySystem
{
    [Dependency] private readonly ADTDrakeAttacksSystem _attacks = default!;
    [Dependency] private readonly ADTDrakeSystem _drake = default!;
    [Dependency] private readonly ADTDrakeSwoopSystem _swoop = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeComponent, ADTDrakeMeleeHitLivingEvent>(OnMeleeHitLiving);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDrakeComponent, HTNComponent>();

        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (now < comp.NextDecisionAt)
                continue;

            comp.NextDecisionAt = now + comp.DecisionInterval;

            if (HasComp<ActorComponent>(uid))
                continue;

            if (!_npc.IsAwake(uid, htn))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (_swoop.IsSwooping(uid))
                continue;

            if (now < comp.NextRangedAt)
                continue;

            if (!_drake.TryGetTarget(uid, out var found) || found is not { } target)
                continue;

            if (_transform.GetGrid(uid) != _transform.GetGrid(target))
            {
                htn.Blackboard.Remove<EntityUid>("Target");
                continue;
            }

            if (IsAdjacent(uid, target))
                continue;

            OpenFire((uid, comp), target);
        }
    }

    private void OnMeleeHitLiving(Entity<ADTDrakeComponent> ent, ref ADTDrakeMeleeHitLivingEvent args)
    {
        if (HasComp<ActorComponent>(ent))
            return;

        if (_timing.CurTime < ent.Comp.NextRangedAt)
            return;

        if (_swoop.IsSwooping(ent))
            return;

        OpenFire(ent, args.Target);
    }

    public void OpenFire(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        _drake.Aggro(ent, target);

        var anger = _drake.CalculateAnger(ent);
        ent.Comp.NextRangedAt = _timing.CurTime + ent.Comp.RangedCooldown;

        if (_random.Prob(Math.Clamp(ent.Comp.LavaSwoopChanceBase + anger / 100f, 0f, 1f)))
        {
            _attacks.LavaSwoop(ent, target);
            return;
        }

        if (_random.Prob(Math.Clamp(ent.Comp.ShootFireChanceBase + anger / 100f, 0f, 1f)))
        {
            _attacks.ShootFireAttack(ent, target);
            return;
        }

        _attacks.FireCone(ent, target);
    }

    private bool IsAdjacent(EntityUid uid, EntityUid target)
    {
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var targetCoords = _transform.GetMapCoordinates(target);
        if (targetCoords.MapId != _transform.GetMapCoordinates(uid).MapId)
            return false;

        var ours = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var theirs = _map.TileIndicesFor(gridUid, grid, targetCoords);
        var delta = theirs - ours;

        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) <= 1;
    }
}

[ByRefEvent]
public readonly record struct ADTDrakeMeleeHitLivingEvent(EntityUid Target);
