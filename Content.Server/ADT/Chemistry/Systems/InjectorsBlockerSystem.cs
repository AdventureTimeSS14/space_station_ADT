using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Events;
using Content.Shared.Inventory;
using Content.Server.Chemistry.Components;
using Content.Server.Medical;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class InjectorsBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorsBlockerComponent, InjectAttemptEvent>(OnInjectAttempt);
        SubscribeLocalEvent<InjectorsBlockerComponent, InventoryRelayedEvent<InjectAttemptEvent>>(OnInjectRelayAttempt);
        SubscribeLocalEvent<InjectorsBlockerComponent, TargetBeforeInjectEvent>(OnTargetBeforeInject);
    }

    private void OnInjectAttempt(EntityUid uid, InjectorsBlockerComponent comp, InjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.UsedInjector != EntityUid.Invalid && InjectorIgnoresBlockers(args.UsedInjector))
            return;

        args.Cancel();
    }

    private void OnInjectRelayAttempt(EntityUid uid, InjectorsBlockerComponent comp, InventoryRelayedEvent<InjectAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        if (args.Args.UsedInjector != EntityUid.Invalid && InjectorIgnoresBlockers(args.Args.UsedInjector))
            return;

        args.Args.Cancel();
    }

    private bool InjectorIgnoresBlockers(EntityUid injectorUid)
    {
        if (TryComp<InjectorComponent>(injectorUid, out var injector))
            return injector.IgnoreBlockers;

        if (TryComp<BaseSolutionInjectOnEventComponent>(injectorUid, out var eventInjector))
            return eventInjector.IgnoreBlockers;

        return false;
    }

    private void OnTargetBeforeInject(EntityUid uid, InjectorsBlockerComponent comp, TargetBeforeInjectEvent args)
    {
        if (args.Cancelled)
            return;

        if (InjectorIgnoresBlockers(args.UsedInjector))
            return;

        args.Cancel();
    }
}

