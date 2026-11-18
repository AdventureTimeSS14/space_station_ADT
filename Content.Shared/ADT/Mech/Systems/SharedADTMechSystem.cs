using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Storage;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Mech.EntitySystems;

public abstract class SharedADTMechSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private VehicleSystem _vehicle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTMechComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<ADTMechComponent, ContainerVehicleEntryAttemptEvent>(OnEntryAttempt);
        SubscribeLocalEvent<ADTMechComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<ADTMechComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<ADTMechComponent, CanDropTargetEvent>(OnCanDrop, after: new[] { typeof(VehicleSystem) });
        SubscribeLocalEvent<ADTMechComponent, DragDropTargetEvent>(OnDragDrop, before: new[] { typeof(VehicleSystem) });
        SubscribeLocalEvent<ADTMechComponent, StorageInteractAttemptEvent>(OnStorageInteract);
        SubscribeLocalEvent<ADTMechComponent, MechTurnLightsEvent>(OnToggleLights);
        SubscribeLocalEvent<ADTMechComponent, MechInhaleEvent>(OnToggleSeal);
        SubscribeLocalEvent<ADTMechComponent, MechOpenEquipmentRadialEvent>(OnOpenRadial);
        SubscribeLocalEvent<ADTMechComponent, MechCycleEquipmentEvent>(OnCycleEquipment);

        SubscribeLocalEvent<MechPilotComponent, GetMeleeWeaponEvent>(OnPilotGetMeleeWeapon);
        SubscribeLocalEvent<MechPilotComponent, CanAttackFromContainerEvent>(OnPilotCanAttackFromContainer);
        SubscribeLocalEvent<MechPilotComponent, AttackAttemptEvent>(OnPilotAttackAttempt);
        SubscribeLocalEvent<MechPilotComponent, ElectrocutionAttemptEvent>(OnPilotElectrocutionAttempt);
        SubscribeLocalEvent<MechPilotComponent, EntRemovedFromContainerMessage>(OnPilotStatusRemoved, after: new[] { typeof(StatusEffectsSystem) });

        SubscribeLocalEvent<MechEquipmentDurabilityComponent, ExaminedEvent>(OnDurabilityExamined);

        SubscribeAllEvent<SelectMechEquipmentEvent>(OnSelectEquipment);
    }

    private void OnDurabilityExamined(Entity<MechEquipmentDurabilityComponent> ent, ref ExaminedEvent args)
    {
        var left = FixedPoint2.Max(0, ent.Comp.MaxDurability - _damageable.GetTotalDamage(ent.Owner));
        var percent = (int) MathF.Round(left.Float() / ent.Comp.MaxDurability.Float() * 100f);
        var key = ent.Comp.Broken ? "adt-mech-equipment-examine-broken" : "adt-mech-equipment-examine-durability";
        args.PushMarkup(Loc.GetString(key, ("percent", percent)));
    }

    private void OnMapInit(Entity<ADTMechComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<MechComponent>(ent, out var mech))
            ent.Comp.Sealed = mech.Airtight;

        _lights.SetEnabled(ent, ent.Comp.LightsOn);
        Dirty(ent);

        if (ent.Comp.Sealed)
        {
            var ev = new MechSealToggledEvent(true);
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnOperatorSet(Entity<ADTMechComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator is { } oldPilot)
            RemovePilot(ent, oldPilot);

        if (args.NewOperator is { } newPilot)
            SetupPilot(ent, newPilot);
    }

    private void SetupPilot(Entity<ADTMechComponent> ent, EntityUid pilot)
    {
        var pilotComp = EnsureComp<MechPilotComponent>(pilot);
        pilotComp.Mech = ent;
        Dirty(pilot, pilotComp);

        EnsureComp<ProtectedFromStepTriggersComponent>(pilot);

        UpdateMouseRotation(pilot);

        _audio.PlayPredicted(ent.Comp.EntrySound, ent, pilot);

        if (_net.IsServer)
        {
            _actions.AddAction(pilot, ref ent.Comp.RadialActionEntity, ent.Comp.RadialAction, ent);
            _actions.AddAction(pilot, ref ent.Comp.LightsActionEntity, ent.Comp.LightsAction, ent);
            _actions.AddAction(pilot, ref ent.Comp.SealActionEntity, ent.Comp.SealAction, ent);
            _actions.AddAction(pilot, ref ent.Comp.ReloadActionEntity, ent.Comp.ReloadAction, ent);
            _actions.SetToggled(ent.Comp.LightsActionEntity, ent.Comp.LightsOn);
            _actions.SetToggled(ent.Comp.SealActionEntity, ent.Comp.Sealed);
            Dirty(ent);
        }

        var ev = new SetupMechUserEvent(pilot);
        RaiseLocalEvent(ent, ref ev);
    }

    private void RemovePilot(Entity<ADTMechComponent> ent, EntityUid pilot)
    {
        RemComp<MechPilotComponent>(pilot);
        RemCompDeferred<ProtectedFromStepTriggersComponent>(pilot);
        RemComp<MouseRotatorComponent>(ent);
        RemComp<NoRotateOnMoveComponent>(ent);

        CloseRadial(ent, pilot);

        var ev = new RemoveMechUserEvent(pilot);
        RaiseLocalEvent(ent, ref ev);
    }

    public void UpdateMouseRotation(EntityUid pilot)
    {
        if (!TryComp<MechPilotComponent>(pilot, out var pilotComp) || !HasComp<ADTMechComponent>(pilotComp.Mech))
            return;

        if (HasComp<MechControlLockedComponent>(pilot))
            return;

        var mech = pilotComp.Mech;
        if (_combatMode.IsInCombatMode(pilot))
        {
            EnsureComp<MouseRotatorComponent>(mech);
            EnsureComp<NoRotateOnMoveComponent>(mech);
            return;
        }

        RemComp<MouseRotatorComponent>(mech);
        RemComp<NoRotateOnMoveComponent>(mech);
    }

    public bool HasMechAccess(EntityUid mech, EntityUid user)
    {
        if (!TryComp<AccessReaderComponent>(mech, out var reader))
            return true;

        return _accessReader.IsAllowed(user, mech, reader);
    }

    private void OnEntryAttempt(Entity<ADTMechComponent> ent, ref ContainerVehicleEntryAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasMechAccess(ent, args.Entering))
            args.Cancelled = true;
    }

    private void OnCanDrop(Entity<ADTMechComponent> ent, ref CanDropTargetEvent args)
    {
        if (!TryComp<MechComponent>(ent, out var mech) || mech.Broken)
            return;

        args.CanDrop |= _vehicle.CanEnter(ent.Owner, args.Dragged);
        args.Handled = true;
    }

    private void OnDragDrop(Entity<ADTMechComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || HasMechAccess(ent, args.Dragged))
            return;

        args.Handled = true;
        NotifyAccessDenied(ent, args.User);
    }

    private void OnGetAltVerbs(Entity<ADTMechComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<MechComponent>(ent, out var mech) || mech.Broken)
            return;

        AddExtraVerbs(ent, mech, args);

        if (_vehicle.HasOperator(ent.Owner) || HasMechAccess(ent, args.User))
            return;

        if (!TryComp<VehicleComponent>(ent, out var vehicle) || _whitelist.IsWhitelistFail(vehicle.OperatorWhitelist, args.User))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("container-vehicle-verb-enter"),
            Message = Loc.GetString("adt-mech-access-denied"),
            Disabled = true,
        });
    }

    protected virtual void AddExtraVerbs(Entity<ADTMechComponent> ent, MechComponent mech, GetVerbsEvent<AlternativeVerb> args)
    {
    }

    private void OnUiMessageAttempt(Entity<ADTMechComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Cancelled || args.UiKey is not MechUiKey.Key || HasMechAccess(ent, args.Actor))
            return;

        args.Cancel();
        NotifyAccessDenied(ent, args.Actor);
    }

    private void NotifyAccessDenied(Entity<ADTMechComponent> ent, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("adt-mech-access-denied"), ent, user);
        _audio.PlayPredicted(ent.Comp.AccessDeniedSound, ent, user);
    }

    private void OnStorageInteract(Entity<ADTMechComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (_vehicle.HasOperator(ent.Owner))
            args.Cancelled = true;
    }

    private void OnToggleLights(Entity<ADTMechComponent> ent, ref MechTurnLightsEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.LightsOn = !ent.Comp.LightsOn;
        Dirty(ent);

        _lights.SetEnabled(ent, ent.Comp.LightsOn);
        _actions.SetToggled(ent.Comp.LightsActionEntity, ent.Comp.LightsOn);

        var sound = ent.Comp.LightsOn ? ent.Comp.LightsOnSound : ent.Comp.LightsOffSound;
        _audio.PlayPredicted(sound, ent, args.Performer);
    }

    private void OnToggleSeal(Entity<ADTMechComponent> ent, ref MechInhaleEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<MechComponent>(ent, out var mech))
            return;

        ent.Comp.Sealed = !ent.Comp.Sealed;
        mech.Airtight = ent.Comp.Sealed;
        Dirty(ent);

        _actions.SetToggled(ent.Comp.SealActionEntity, ent.Comp.Sealed);
        _audio.PlayPredicted(ent.Comp.SealSound, ent, args.Performer);

        var popup = ent.Comp.Sealed ? "adt-mech-sealed" : "adt-mech-unsealed";
        _popup.PopupClient(Loc.GetString(popup), ent, args.Performer);

        var ev = new MechSealToggledEvent(ent.Comp.Sealed);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnOpenRadial(Entity<ADTMechComponent> ent, ref MechOpenEquipmentRadialEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_timing.IsFirstTimePredicted)
            OpenRadial(ent, args.Performer);
    }

    protected virtual void OpenRadial(EntityUid mech, EntityUid pilot)
    {
    }

    protected virtual void CloseRadial(EntityUid mech, EntityUid pilot)
    {
    }

    private void OnCycleEquipment(Entity<ADTMechComponent> ent, ref MechCycleEquipmentEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<MechComponent>(ent, out var mech))
            return;

        var usable = GetSelectableEquipment(mech);
        EntityUid? next = null;

        if (usable.Count > 0)
        {
            var index = mech.CurrentSelectedEquipment is { } current ? usable.IndexOf(current) : -1;
            index++;
            next = index < usable.Count ? usable[index] : null;
        }

        SelectEquipment((ent, mech), next, args.Performer);
    }

    private void OnSelectEquipment(SelectMechEquipmentEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var mechUid = GetEntity(ev.Mech);
        if (!TryComp<MechPilotComponent>(user, out var pilot) || pilot.Mech != mechUid)
            return;

        if (HasComp<MechControlLockedComponent>(user))
            return;

        if (!TryComp<MechComponent>(mechUid, out var mech))
            return;

        var equipment = GetEntity(ev.Equipment);
        if (equipment != null && !GetSelectableEquipment(mech).Contains(equipment.Value))
            return;

        SelectEquipment((mechUid, mech), equipment, user);
    }

    public List<EntityUid> GetSelectableEquipment(MechComponent mech)
    {
        return mech.EquipmentContainer.ContainedEntities.Where(CanSelect).ToList();
    }

    public bool CanSelect(EntityUid equipment)
    {
        if (HasComp<MechPassiveEquipmentComponent>(equipment))
            return false;

        if (TryComp<MechEquipmentDurabilityComponent>(equipment, out var durability) && durability.Broken)
            return false;

        return true;
    }

    public void SelectEquipment(Entity<MechComponent> mech, EntityUid? equipment, EntityUid? user)
    {
        mech.Comp.CurrentSelectedEquipment = equipment;
        Dirty(mech);

        var popup = equipment != null
            ? Loc.GetString("mech-equipment-select-popup", ("item", equipment.Value))
            : Loc.GetString("mech-equipment-select-none-popup");

        if (user != null)
            _popup.PopupClient(popup, mech, user.Value);
    }

    private void OnPilotGetMeleeWeapon(Entity<MechPilotComponent> ent, ref GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<MechControlLockedComponent>(ent))
            return;

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        args.Weapon = mech.CurrentSelectedEquipment ?? ent.Comp.Mech;
        args.Handled = true;
    }

    private void OnPilotCanAttackFromContainer(Entity<MechPilotComponent> ent, ref CanAttackFromContainerEvent args)
    {
        if (!HasComp<MechControlLockedComponent>(ent))
            args.CanAttack = true;
    }

    private void OnPilotAttackAttempt(Entity<MechPilotComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target == ent.Comp.Mech)
            args.Cancel();

        if (TryComp<MechComponent>(ent.Comp.Mech, out var mech) && mech.Energy <= 0)
            args.Cancel();

        if (HasComp<MechControlLockedComponent>(ent))
            args.Cancel();
    }

    private void OnPilotElectrocutionAttempt(Entity<MechPilotComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (HasComp<ADTMechComponent>(ent.Comp.Mech))
            args.Cancel();
    }

    private void OnPilotStatusRemoved(Entity<MechPilotComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != StatusEffectContainerComponent.ContainerId)
            return;

        _actionBlocker.UpdateCanMove(ent);
        _vehicle.RefreshCanRun(ent.Comp.Mech);
    }

    public EntityUid? GetEquipmentMech(EntityUid equipment)
    {
        if (!_container.TryGetContainingContainer(equipment, out var container))
            return null;

        return HasComp<MechComponent>(container.Owner) ? container.Owner : null;
    }

    public void ForceDeselect(EntityUid mech, EntityUid equipment)
    {
        if (!TryComp<MechComponent>(mech, out var mechComp) || mechComp.CurrentSelectedEquipment != equipment)
            return;

        SelectEquipment((mech, mechComp), null, _vehicle.GetOperatorOrNull(mech));
    }
}
