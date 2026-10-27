using Content.Shared.Body;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;

namespace Content.Shared.ADT.Body.Legs;

public sealed class LegMobilitySystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<LegOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<LegMobilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<LegMobilityComponent, StandAttemptEvent>(OnStandAttempt);
    }

    private void OnGotInserted(Entity<LegOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!HasComp<StandingStateComponent>(args.Target))
            return;

        var legs = EnsureComp<LegMobilityComponent>(args.Target);
        legs.Present |= ent.Comp.Part;
        Dirty(args.Target, legs);

        _movement.RefreshMovementSpeedModifiers(args.Target);

        if (legs.ForcedDown && !CantStand(legs))
        {
            legs.ForcedDown = false;
            _standing.Stand(args.Target);
        }
    }

    private void OnGotRemoved(Entity<LegOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (LifeStage(args.Target) >= EntityLifeStage.Terminating)
            return;

        if (!TryComp<LegMobilityComponent>(args.Target, out var legs))
            return;

        legs.Present &= ~ent.Comp.Part;
        Dirty(args.Target, legs);

        _movement.RefreshMovementSpeedModifiers(args.Target);

        if (!CantStand(legs))
            return;

        legs.ForcedDown = true;
        _standing.Down(args.Target, force: true);
    }

    private static bool CantStand(LegMobilityComponent legs)
    {
        return (legs.Present & (LegParts.LegLeft | LegParts.LegRight)) == 0;
    }

    private void OnStandAttempt(Entity<LegMobilityComponent> ent, ref StandAttemptEvent args)
    {
        if (CantStand(ent.Comp))
            args.Cancel();
    }

    private void OnRefreshSpeed(Entity<LegMobilityComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var legs = ent.Comp;

        if (CantStand(legs))
        {
            args.ModifySpeed(legs.CrawlModifier);
            return;
        }

        args.ModifySpeed(Side(legs, LegParts.LegLeft, LegParts.FootLeft) * Side(legs, LegParts.LegRight, LegParts.FootRight));
    }

    private static float Side(LegMobilityComponent legs, LegParts leg, LegParts foot)
    {
        if ((legs.Present & leg) == 0)
            return legs.MissingLegModifier;

        return (legs.Present & foot) == 0 ? legs.MissingFootModifier : 1f;
    }
}
