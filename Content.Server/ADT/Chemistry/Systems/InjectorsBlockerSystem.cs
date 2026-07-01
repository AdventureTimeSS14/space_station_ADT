using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.Inventory;
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
        args.Cancel();
    }

    private void OnInjectRelayAttempt(EntityUid uid, InjectorsBlockerComponent comp, InventoryRelayedEvent<InjectAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnTargetBeforeInject(EntityUid uid, InjectorsBlockerComponent comp, TargetBeforeInjectEvent args)
    {
        args.Cancel();
    }
}

