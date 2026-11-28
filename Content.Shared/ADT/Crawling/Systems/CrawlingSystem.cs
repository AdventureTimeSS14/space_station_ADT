using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared.ADT.Crawling;

public sealed class CrawlingSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    private static readonly TimeSpan ExplosionKnockdownTime = TimeSpan.FromSeconds(2);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StandingStateComponent, GetExplosionResistanceEvent>(OnExplosion);
        SubscribeLocalEvent<ClimbableComponent, AttemptClimbEvent>(OnClimbAttempt);
    }

    private void OnExplosion(Entity<StandingStateComponent> ent, ref GetExplosionResistanceEvent args)
    {
        var ev = new ExplosionDownAttemptEvent(args.ExplosionPrototype);
        RaiseLocalEvent(ent, ref ev);

        if (TryComp<InventoryComponent>(ent, out var inventory))
            _inventory.RelayEvent((ent.Owner, inventory), ref ev);

        if (ev.Cancelled)
            return;

        _stun.TryKnockdown(ent.Owner, ExplosionKnockdownTime, true);
    }

    private void OnClimbAttempt(Entity<ClimbableComponent> ent, ref AttemptClimbEvent args)
    {
        if (args.Cancelled || args.User != args.Climber || !_standing.IsDown(args.Climber))
            return;

        args.Cancelled = true;
    }
}
