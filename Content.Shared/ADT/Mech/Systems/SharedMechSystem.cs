using Content.Shared.ADT.Mech;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Overlays;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SelectMechEquipmentEvent>(OnMechEquipSelected);

        SubscribeLocalEvent<MechComponent, MechGrabberEjectMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<MechComponent, MechSoundboardPlayMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<MechComponent, MechGunReloadMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<MechComponent, StorageInteractAttemptEvent>(OnStorageInteract);

        SubscribeLocalEvent<MechPilotComponent, GetMeleeWeaponEvent>(OnPilotGetMeleeWeapon);
        SubscribeLocalEvent<MechPilotComponent, CanAttackFromContainerEvent>(OnPilotCanAttackFromContainer);
        SubscribeLocalEvent<MechPilotComponent, AttackAttemptEvent>(OnPilotAttackAttempt);
    }

    public EntityUid? GetPilot(EntityUid mech)
    {
        return Vehicle.GetOperatorOrNull(mech);
    }

    protected virtual void AddAlternativeVerbsADT(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
    }

    private void SetupUserADT(EntityUid mech, EntityUid pilot, MechComponent component)
    {
        var rider = EnsureComp<MechPilotComponent>(pilot);
        rider.Mech = mech;
        Dirty(pilot, rider);

        EnsureComp<ProtectedFromStepTriggersComponent>(pilot);
    }

    private void SetupUserActionsADT(EntityUid mech, EntityUid pilot, MechComponent component)
    {
        _actions.AddAction(pilot, ref component.MechInhaleActionEntity, component.MechInhaleAction, mech);
        _actions.AddAction(pilot, ref component.MechTurnLightsActionEntity, component.MechTurnLightsAction, mech);

        var ev = new SetupMechUserEvent(pilot);
        RaiseLocalEvent(mech, ref ev);
    }

    private void RemoveUserADT(EntityUid mech, EntityUid pilot)
    {
        RemComp<MechPilotComponent>(pilot);
        RemCompDeferred<ProtectedFromStepTriggersComponent>(pilot);
        RemComp<NoRotateOnMoveComponent>(mech);
        RemComp<ShowHealthBarsComponent>(pilot);

        if (_net.IsClient && _timing.IsFirstTimePredicted)
        {
            var closeEv = new CloseMechMenuEvent();
            RaiseLocalEvent(mech, closeEv);
        }

        var ev = new RemoveMechUserEvent(pilot);
        RaiseLocalEvent(mech, ref ev);
    }

    private void OnPilotGetMeleeWeapon(EntityUid uid, MechPilotComponent component, GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<MechControlLockedComponent>(uid))
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Weapon = mech.CurrentSelectedEquipment ?? component.Mech;
        args.Handled = true;
    }

    private void OnPilotCanAttackFromContainer(EntityUid uid, MechPilotComponent component, CanAttackFromContainerEvent args)
    {
        if (!HasComp<MechControlLockedComponent>(uid))
            args.CanAttack = true;
    }

    private void OnPilotAttackAttempt(EntityUid uid, MechPilotComponent component, AttackAttemptEvent args)
    {
        if (args.Target == component.Mech)
            args.Cancel();

        if (TryComp<MechComponent>(component.Mech, out var mech) && mech.Energy <= 0)
            args.Cancel();

        if (HasComp<MechControlLockedComponent>(uid))
            args.Cancel();
    }

    private void OnMechEquipSelected(SelectMechEquipmentEvent ev)
    {
        var uid = GetEntity(ev.User);

        if (!TryComp<MechPilotComponent>(uid, out var pilot))
            return;

        var mechUid = pilot.Mech;
        if (!TryComp<MechComponent>(mechUid, out var mech))
            return;

        if (ev.Equipment == null)
        {
            mech.CurrentSelectedEquipment = null;

            var popup = Loc.GetString("mech-equipment-select-none-popup");

            if (_timing.IsFirstTimePredicted)
                _popup.PopupEntity(popup, uid, uid);

            if (_net.IsServer)
                Dirty(mechUid, mech);
            return;
        }

        var entity = GetEntity(ev.Equipment.Value);

        if (!mech.EquipmentContainer.Contains(entity))
            Log.Error("Mech does not have selected equipment");
        mech.CurrentSelectedEquipment = entity;

        var popupString = mech.CurrentSelectedEquipment != null
            ? Loc.GetString("mech-equipment-select-popup", ("item", mech.CurrentSelectedEquipment))
            : Loc.GetString("mech-equipment-select-none-popup");

        if (_timing.IsFirstTimePredicted)
            _popup.PopupEntity(popupString, uid, uid);

        if (_net.IsServer)
            Dirty(mechUid, mech);
    }

    public virtual void UpdateUserInterfaceByEquipment(EntityUid uid)
    {
    }

    protected void OnStorageInteract(EntityUid uid, MechComponent component, ref StorageInteractAttemptEvent args)
    {
        if (Vehicle.HasOperator(uid))
            args.Cancelled = true;
    }

    private void ReceiveEquipmentUiMesssages<T>(EntityUid uid, MechComponent component, T args) where T : MechEquipmentUiMessage
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var ev = new MechEquipmentUiMessageRelayEvent(args, GetNetEntity(GetPilot(uid)));
        var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        var argEquip = GetEntity(args.Equipment);

        foreach (var equipment in allEquipment)
        {
            if (argEquip == equipment)
                RaiseLocalEvent(equipment, ev);
        }
    }
}
