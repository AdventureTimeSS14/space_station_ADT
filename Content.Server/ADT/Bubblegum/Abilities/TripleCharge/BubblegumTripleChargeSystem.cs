using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Bubblegum.Abilities;

public sealed class BubblegumTripleChargeSystem : EntitySystem
{
    [Dependency] private readonly BubblegumChargeSystem _charge = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    private const float TravelBuffer = 0.6f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumTripleChargeComponent, BubblegumTripleChargeActionEvent>(OnAction);
        SubscribeLocalEvent<BubblegumTripleChargeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<BubblegumTripleChargeComponent> ent, ref ComponentShutdown args)
    {
        ClearPending(ent);
    }

    private void OnAction(Entity<BubblegumTripleChargeComponent> ent, ref BubblegumTripleChargeActionEvent args)
    {
        if (args.Handled)
            return;

        var target = _transform.ToMapCoordinates(args.Target);
        if (target.MapId == MapId.Nullspace)
            return;

        if (ent.Comp.Delays.Count == 0)
            return;

        args.Handled = true;

        if (!HasComp<ActorComponent>(ent))
        {
            StartNpcTripleCharge(ent, target);
            return;
        }

        ent.Comp.PendingPlayerTargets.Add(target);
        ent.Comp.PendingMarkers.Add(SpawnMarker(ent.Comp.MarkerPrototype, target));

        if (ent.Comp.PendingPlayerTargets.Count >= ent.Comp.Delays.Count)
        {
            ExecutePlayerSeries(ent, ent.Comp.PendingPlayerTargets);
            ClearPending(ent);
            _actions.SetUseDelay(args.Action.AsNullable(), ent.Comp.FullCooldown);
            return;
        }

        _actions.SetUseDelay(args.Action.AsNullable(), ent.Comp.PlayerCooldownBetweenClicks);
    }

    private EntityUid SpawnMarker(string proto, MapCoordinates coords)
    {
        var marker = Spawn(proto, coords, rotation: Angle.Zero);
        var vis = EnsureComp<VisibilityComponent>(marker);
        _visibility.SetLayer((marker, vis), (ushort)VisibilityFlags.Bubblegum);
        return marker;
    }

    public void StartNpcTripleCharge(Entity<BubblegumTripleChargeComponent> ent, MapCoordinates fallback)
    {
        EntityUid? targetEntity = null;
        if (TryComp<HTNComponent>(ent, out var htn)
            && htn.Blackboard.TryGetValue<EntityUid>("Target", out var htnTarget, EntityManager)
            && !TerminatingOrDeleted(htnTarget))
        {
            targetEntity = htnTarget;
        }

        var cumulative = 0f;
        for (var i = 0; i < ent.Comp.Delays.Count; i++)
        {
            var delay = ent.Comp.Delays[i];
            _charge.BeginCharge(
                ent.Owner,
                fallback,
                delaySeconds: cumulative + delay,
                speed: ent.Comp.ChargeSpeed,
                telegraphProto: ent.Comp.TelegraphPrototype,
                trampleDamage: 30f,
                telegraphLeadSeconds: cumulative,
                targetEntity: targetEntity);

            cumulative += delay + TravelBuffer;
        }
    }

    private void ExecutePlayerSeries(Entity<BubblegumTripleChargeComponent> ent, List<MapCoordinates> targets)
    {
        var cumulative = 0f;
        for (var i = 0; i < targets.Count; i++)
        {
            var delay = ent.Comp.Delays[i];
            _charge.BeginCharge(
                ent.Owner,
                targets[i],
                delaySeconds: cumulative + delay,
                speed: ent.Comp.ChargeSpeed,
                telegraphProto: ent.Comp.TelegraphPrototype,
                trampleDamage: 30f,
                telegraphLeadSeconds: cumulative);

            cumulative += delay + TravelBuffer;
        }
    }

    private void ClearPending(Entity<BubblegumTripleChargeComponent> ent)
    {
        foreach (var marker in ent.Comp.PendingMarkers)
        {
            if (!TerminatingOrDeleted(marker))
                QueueDel(marker);
        }
        ent.Comp.PendingMarkers.Clear();
        ent.Comp.PendingPlayerTargets.Clear();
    }
}
