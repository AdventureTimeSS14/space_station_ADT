using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class InjectorsBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorsBlockerComponent, InjectAttemptEvent>(OnInjectAttempt);
        SubscribeLocalEvent<InjectorsBlockerComponent, InventoryRelayedEvent<InjectAttemptEvent>>(OnInjectRelayAttempt);
    }

    private void OnInjectAttempt(EntityUid uid, InjectorsBlockerComponent comp, InjectAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInjectRelayAttempt(EntityUid uid, InjectorsBlockerComponent comp, InventoryRelayedEvent<InjectAttemptEvent> args)
    {
        args.Args.Cancel();
    }
}
