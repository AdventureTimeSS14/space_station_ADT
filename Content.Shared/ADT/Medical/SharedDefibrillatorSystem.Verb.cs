using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Medical;

public abstract partial class SharedDefibrillatorSystem
{
    [Dependency] private InventorySystem _inventory = default!;

    [SubscribeLocalEvent]
    private void AddZapVerb(Entity<DefibrillatorComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
    {
        if (!args.Args.CanInteract || !args.Args.CanAccess || !HasComp<MobStateComponent>(args.Args.Target))
            return;

        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) || (slot.SlotFlags & SlotFlags.GLOVES) == 0)
            return;

        var target = args.Args.Target;
        var user = args.Args.User;

        var verb = new InnateVerb
        {
            Act = () => TryStartZap(ent.AsNullable(), target, user),
            Text = Loc.GetString("defibrillator-verb-zap"),
            IconEntity = GetNetEntity(ent),
            Priority = 2,
        };
        args.Args.Verbs.Add(verb);
    }
}
