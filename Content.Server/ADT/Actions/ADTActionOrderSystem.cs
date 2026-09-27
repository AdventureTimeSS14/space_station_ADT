using System.Collections.Immutable;
using Content.Shared.ADT.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.ADT.Actions;

public sealed class ADTActionOrderSystem : EntitySystem
{
    private const int MaxEntries = 128;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTActionOrderChangeEvent>(OnOrderChanged);

        SubscribeLocalEvent<ADTActionOrderComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        var order = EnsureComp<ADTActionOrderComponent>(ev.Entity);
        Dirty(ev.Entity, order);
    }

    private void OnGetStateAttempt(Entity<ADTActionOrderComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity != ent.Owner)
            args.Cancelled = true;
    }

    private void OnOrderChanged(ADTActionOrderChangeEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (ev.Order.Count > MaxEntries || ev.Removed.Count > MaxEntries)
            return;

        var order = EnsureComp<ADTActionOrderComponent>(player);

        order.Order = ev.Order.ToImmutableArray();
        order.Removed = ev.Removed.ToImmutableArray();

        Dirty(player, order);
    }
}
