using Content.Shared.ADT.Mech.Components;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechToolSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, GetUsedEntityEvent>(OnGetUsedEntity);
        SubscribeLocalEvent<MechComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);

        SubscribeLocalEvent<MechToolComponent, ToolUseAttemptEvent>(OnToolUseAttempt);
    }

    private void OnGetUsedEntity(EntityUid uid, MechComponent comp, ref GetUsedEntityEvent args)
    {
        if (args.Handled)
            return;

        if (comp.Energy <= 0)
            return;

        if (comp.CurrentSelectedEquipment is not { } equipment || !HasComp<MechToolComponent>(equipment))
            return;

        args.Used = equipment;
    }

    private void OnBeforeInteractHand(EntityUid uid, MechComponent comp, BeforeInteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (comp.Energy <= 0)
            return;

        if (comp.CurrentSelectedEquipment is not { } equipment)
            return;

        if (HasComp<MechToolComponent>(equipment))
            return;

        args.Handled = true;

        var ev = new UserActivateInWorldEvent(uid, args.Target, true);
        RaiseLocalEvent(equipment, ev);
    }

    private void OnToolUseAttempt(EntityUid uid, MechToolComponent comp, ToolUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var owner = CompOrNull<MechEquipmentComponent>(uid)?.EquipmentOwner;

        if (owner != args.User || !TryComp<MechComponent>(owner, out var mech))
        {
            args.Cancel();
            return;
        }

        if (comp.EnergyCost <= 0)
            return;

        if (mech.Energy < comp.EnergyCost)
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupEntity(Loc.GetString("adt-mech-tool-no-energy"), owner.Value, owner.Value);

            args.Cancel();
            return;
        }

        if (_net.IsServer)
            _mech.TryChangeEnergy(owner.Value, -comp.EnergyCost, mech);
    }
}
