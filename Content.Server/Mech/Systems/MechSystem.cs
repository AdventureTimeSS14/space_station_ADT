using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Mech.Components;
<<<<<<< ours
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
||||||| base
using Content.Shared.ActionBlocker;
=======
using Content.Shared.Atmos;
>>>>>>> theirs
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
<<<<<<< ours
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
||||||| base
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
=======
>>>>>>> theirs
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Wires;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
<<<<<<< ours
using Robust.Shared.Random;
||||||| base
using System.Linq;
using Content.Shared.Atmos;
=======
>>>>>>> theirs

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class MechSystem : SharedMechSystem
{
<<<<<<< ours
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
||||||| base
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
=======
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedToolSystem _toolSystem = default!;
>>>>>>> theirs

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

<<<<<<< ours
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MechComponent, EntInsertedIntoContainerMessage>(OnInsertBattery);
        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<MechComponent, RemoveBatteryEvent>(OnRemoveBattery);
        SubscribeLocalEvent<MechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<MechComponent, MechExitEvent>(OnMechExit);

        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);

        SubscribeLocalEvent<MechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);
        SubscribeLocalEvent<MechPilotComponent, UpdateCanMoveEvent>(OnMechPilotCanMoveEvent); // ADT-Tweak
        SubscribeLocalEvent<MovementRelayTargetComponent, UpdateCanMoveEvent>(OnMechRelayTargetCanMoveEvent); // ADT-Tweak


        SubscribeLocalEvent<MechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        // SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnInhale);  // ADT Commented
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);

        SubscribeLocalEvent<MechAirComponent, GetFilterAirEvent>(OnGetFilterAir);

        #region Equipment UI message relays
        // SubscribeLocalEvent<MechComponent, MechGrabberEjectMessage>(ReceiveEquipmentUiMesssages);    // ADT - Moved to Shared
        // SubscribeLocalEvent<MechComponent, MechSoundboardPlayMessage>(ReceiveEquipmentUiMesssages);  // ADT - Moved to Shared
        #endregion

        InitializeADT();    // ADT tweak
    }

    private void OnMechCanMoveEvent(EntityUid uid, MechComponent component, UpdateCanMoveEvent args)
||||||| base
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MechComponent, EntInsertedIntoContainerMessage>(OnInsertBattery);
        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<MechComponent, RemoveBatteryEvent>(OnRemoveBattery);
        SubscribeLocalEvent<MechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<MechComponent, MechExitEvent>(OnMechExit);

        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);

        SubscribeLocalEvent<MechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);


        SubscribeLocalEvent<MechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);

        SubscribeLocalEvent<MechAirComponent, GetFilterAirEvent>(OnGetFilterAir);

        #region Equipment UI message relays
        SubscribeLocalEvent<MechComponent, MechGrabberEjectMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<MechComponent, MechSoundboardPlayMessage>(ReceiveEquipmentUiMesssages);
        #endregion
    }

    private void OnMechCanMoveEvent(EntityUid uid, MechComponent component, UpdateCanMoveEvent args)
=======
    [SubscribeLocalEvent]
    private void OnMechCanMoveEvent(Entity<MechComponent> ent, ref VehicleCanRunEvent args)
>>>>>>> theirs
    {
        if (ent.Comp.Broken || ent.Comp.Integrity <= 0 || ent.Comp.Energy <= 0)
            args.CanRun = false;
    }

<<<<<<< ours
    // ADT-Tweak start: stop move zero power cell
    private void OnMechPilotCanMoveEvent(EntityUid uid, MechPilotComponent component, UpdateCanMoveEvent args)
    {
        if (TryComp<MechComponent>(component.Mech, out var mech) && mech.Energy <= 0)
            args.Cancel();
    }

    private void OnMechRelayTargetCanMoveEvent(EntityUid uid, MovementRelayTargetComponent component, UpdateCanMoveEvent args)
    {
        if (TryComp<MechComponent>(uid, out var mech) && mech.Energy <= 0)
            args.Cancel();
    }
     // ADT-Tweak end

||||||| base
=======
    [SubscribeLocalEvent]
>>>>>>> theirs
    private void OnInteractUsing(EntityUid uid, MechComponent component, InteractUsingEvent args)
    {
        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        // ADT-Tweak-Start
        if (HasComp<MechEquipmentComponent>(args.Used))
            return;
        // ADT-Tweak-End

        if (component.BatterySlot.ContainedEntity == null && TryComp<BatteryComponent>(args.Used, out var battery))
        {
            InsertBattery(uid, args.Used, component, battery);
            return;
        }

        if (_toolSystem.HasQuality(args.Used, PryingQuality) && component.BatterySlot.ContainedEntity != null)
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.BatteryRemovalDelay,
                new RemoveBatteryEvent(), uid, target: uid, used: args.Used)
            {
                BreakOnMove = true
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
        }
    }

    [SubscribeLocalEvent]
    private void OnInsertBattery(EntityUid uid, MechComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container != component.BatterySlot || !TryComp<BatteryComponent>(args.Entity, out var battery))
            return;

        component.Energy = _battery.GetCharge((args.Entity, battery));
        component.MaxEnergy = battery.MaxCharge;

        Dirty(uid, component);
        Vehicle.RefreshCanRun(uid);
    }

    [SubscribeLocalEvent]
    private void OnRemoveBattery(EntityUid uid, MechComponent component, RemoveBatteryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        RemoveBattery(uid, component);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, MechComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);
        // TODO: this should use containerfill?
        foreach (var equipment in component.StartingEquipment)
        {
            var ent = Spawn(equipment, xform.Coordinates);
            InsertEquipment(uid, ent, component);
        }

        // TODO: this should just be damage and battery
        component.Integrity = component.MaxIntegrity;
        component.Energy = component.MaxEnergy;

        Vehicle.RefreshCanRun(uid);
        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnRemoveEquipmentMessage(EntityUid uid, MechComponent component, MechEquipmentRemoveMessage args)
    {
        var equip = GetEntity(args.Equipment);

        if (!Exists(equip) || Deleted(equip))
            return;

        if (!component.EquipmentContainer.ContainedEntities.Contains(equip))
            return;

        RemoveEquipment(uid, equip, component);
        UpdateUserInterface(uid);   // ADT Mech
    }

<<<<<<< ours
    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(uid, component, args.Performer); // ADT-Mech-Tweak
    }

    private void OnToolUseAttempt(EntityUid uid, MechPilotComponent component, ref ToolUserAttemptUseEvent args)
||||||| base
    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(uid, component);
    }

    private void OnToolUseAttempt(EntityUid uid, MechPilotComponent component, ref ToolUserAttemptUseEvent args)
=======
    [SubscribeLocalEvent]
    private void OnToolUseAttempt(Entity<VehicleOperatorComponent> ent, ref ToolUserAttemptUseEvent args)
>>>>>>> theirs
    {
        if (ent.Comp.Vehicle is { } vehicle && args.Target == vehicle)
            args.Cancelled = true;
    }

<<<<<<< ours
    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Broken)
            return;

        _cockpit.AddPassengerVerb(uid, component, args); // ADT-Mech-Tweak

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            var openUiVerb = new AlternativeVerb //can't hijack someone else's mech
            {
                Act = () => ToggleMechUi(uid, component, args.User),
                Text = Loc.GetString("mech-ui-open-verb")
            };
            args.Verbs.Add(enterVerb);
            args.Verbs.Add(openUiVerb);
        }
        else if (!IsEmpty(component))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () =>
                {
                    if (args.User == uid || args.User == component.PilotSlot.ContainedEntity)
                    {
                        TryEject(uid, component);
                        return;
                    }

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.ExitDelay, new MechExitEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };
                    _popup.PopupEntity(Loc.GetString("mech-eject-pilot-alert", ("item", uid), ("user", Identity.Entity(args.User, EntityManager))), uid, PopupType.Large);

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnMechEntry(EntityUid uid, MechComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFail(component.PilotWhitelist, args.User))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", uid)), Identity.Entity(args.User, EntityManager));
            return;
        }
        // ADT Content start
        if (TryComp<AccessReaderComponent>(uid, out var accesscomponent) && !_accessReader.IsAllowed(args.User, uid, accesscomponent))
        {
            _popup.PopupEntity(Loc.GetString("gateway-access-denied"), args.User);
            _audio.PlayPvs(component.AccessDeniedSound, uid);
            args.Handled = true;
            return;
        }
        // ADT Content end

        TryInsert(uid, args.Args.User, component);
        _actionBlocker.UpdateCanMove(uid);
        _actionBlocker.UpdateCanMove(args.Args.User); // ADT-Tweak

        args.Handled = true;
    }

    private void OnMechExit(EntityUid uid, MechComponent component, MechExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        TryEject(uid, component);

        args.Handled = true;
    }

||||||| base
    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Broken)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            var openUiVerb = new AlternativeVerb //can't hijack someone else's mech
            {
                Act = () => ToggleMechUi(uid, component, args.User),
                Text = Loc.GetString("mech-ui-open-verb")
            };
            args.Verbs.Add(enterVerb);
            args.Verbs.Add(openUiVerb);
        }
        else if (!IsEmpty(component))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () =>
                {
                    if (args.User == uid || args.User == component.PilotSlot.ContainedEntity)
                    {
                        TryEject(uid, component);
                        return;
                    }

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.ExitDelay, new MechExitEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };
                    _popup.PopupEntity(Loc.GetString("mech-eject-pilot-alert", ("item", uid), ("user", Identity.Entity(args.User, EntityManager))), uid, PopupType.Large);

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnMechEntry(EntityUid uid, MechComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFail(component.PilotWhitelist, args.User))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", uid)), Identity.Entity(args.User, EntityManager));
            return;
        }

        TryInsert(uid, args.Args.User, component);
        _actionBlocker.UpdateCanMove(uid);

        args.Handled = true;
    }

    private void OnMechExit(EntityUid uid, MechComponent component, MechExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        TryEject(uid, component);

        args.Handled = true;
    }

=======
    [SubscribeLocalEvent]
>>>>>>> theirs
    private void OnDamageChanged(EntityUid uid, MechComponent component, DamageChangedEvent args)
    {
        var integrity = component.MaxIntegrity - _damageable.GetTotalDamage((uid, args.Damageable));
        SetIntegrity(uid, integrity, component);
<<<<<<< ours

        // ADT Mech start
        if (component.Integrity <= component.DamageToDesEqi && !component.Broken && _random.Prob(0.5f) && component.CurrentSelectedEquipment != null)
        {
            var ev = new MechEquipmentDestroyedEvent();
            RaiseLocalEvent(uid, ref ev);
        }
        // ADT Mech end

        if (args.DamageIncreased &&
            args.DamageDelta != null &&
            component.PilotSlot.ContainedEntity != null)
        {
            var damage = args.DamageDelta * component.MechToPilotDamageMultiplier;
            _damageable.ChangeDamage(component.PilotSlot.ContainedEntity.Value, damage);
        }

        _cockpit.RelayDamageToPassenger(uid, component, args); // ADT-Mech-Tweak

        // ADT-Tweak start: stop move zero power cell
        _actionBlocker.UpdateCanMove(uid);
        if (component.PilotSlot.ContainedEntity != null)
            _actionBlocker.UpdateCanMove(component.PilotSlot.ContainedEntity.Value);
        // ADT-Tweak end
||||||| base

        if (args.DamageIncreased &&
            args.DamageDelta != null &&
            component.PilotSlot.ContainedEntity != null)
        {
            var damage = args.DamageDelta * component.MechToPilotDamageMultiplier;
            _damageable.ChangeDamage(component.PilotSlot.ContainedEntity.Value, damage);
        }
=======
>>>>>>> theirs
    }

    [SubscribeLocalEvent]
    private void RelayGrabberUiMessage(EntityUid uid, MechComponent component, ref MechGrabberEjectMessage args)
    {
        ReceiveEquipmentUiMesssages(component, args);
    }

    [SubscribeLocalEvent]
    private void RelaySoundboardUiMessage(EntityUid uid, MechComponent component, ref MechSoundboardPlayMessage args)
    {
        ReceiveEquipmentUiMesssages(component, args);
    }

<<<<<<< ours
    // ADT Moved to shared
    // private void ReceiveEquipmentUiMesssages<T>(EntityUid uid, MechComponent component, T args) where T : MechEquipmentUiMessage
    // {
    //     var ev = new MechEquipmentUiMessageRelayEvent(args);
    //     var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
    //     var argEquip = GetEntity(args.Equipment);
||||||| base
    private void ReceiveEquipmentUiMesssages<T>(EntityUid uid, MechComponent component, T args) where T : MechEquipmentUiMessage
    {
        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        var argEquip = GetEntity(args.Equipment);
=======
    private void ReceiveEquipmentUiMesssages<T>(MechComponent component, T args) where T : MechEquipmentUiMessage
    {
        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        var argEquip = GetEntity(args.Equipment);
>>>>>>> theirs

    //     foreach (var equipment in allEquipment)
    //     {
    //         if (argEquip == equipment)
    //             RaiseLocalEvent(equipment, ev);
    //     }
    // }

    public override void UpdateUserInterface(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.UpdateUserInterface(uid, component);

        var ev = new MechEquipmentUiStateReadyEvent();
        foreach (var ent in component.EquipmentContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, ev);
        }

        var state = new MechBoundUiState
        {
            EquipmentStates = ev.States
        };
        Dirty(uid, component);  // ADT Mech

        _ui.SetUiState(uid, MechUiKey.Key, state);
    }

    public override void BreakMech(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // ADT-Tweak start: stop move zero power cell - обновляем ДО катапультирования
        if (component.PilotSlot.ContainedEntity != null)
            _actionBlocker.UpdateCanMove(component.PilotSlot.ContainedEntity.Value);
        // ADT-Tweak end

        base.BreakMech(uid, component);

        _ui.CloseUi(uid, MechUiKey.Key);
        Vehicle.RefreshCanRun(uid);
    }

    public override bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryChangeEnergy(uid, delta, component))
            return false;

        var battery = component.BatterySlot.ContainedEntity;
        if (battery == null)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        _battery.ChangeCharge((battery.Value, batteryComp), delta.Float());
        var charge = _battery.GetCharge((battery.Value, batteryComp));
        if (charge != component.Energy) //if there's a discrepency, we have to resync them
        {
            Log.Debug($"Battery charge was not equal to mech charge. Battery {charge}. Mech {component.Energy}");
            component.Energy = charge;
            Dirty(uid, component);
        }
<<<<<<< ours
        _actionBlocker.UpdateCanMove(uid);
        // ADT-Tweak start: stop move zero power cell
        if (component.PilotSlot.ContainedEntity != null)
            _actionBlocker.UpdateCanMove(component.PilotSlot.ContainedEntity.Value);
        // ADT-Tweak end

||||||| base
        _actionBlocker.UpdateCanMove(uid);
=======
        Vehicle.RefreshCanRun(uid);
>>>>>>> theirs
        return true;
    }

    public void InsertBattery(EntityUid uid, EntityUid toInsert, MechComponent? component = null, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!Resolve(toInsert, ref battery, false))
            return;

        _container.Insert(toInsert, component.BatterySlot);
        component.Energy = _battery.GetCharge((toInsert, battery));
        component.MaxEnergy = battery.MaxCharge;

<<<<<<< ours
        _actionBlocker.UpdateCanMove(uid);
        // ADT-Tweak start: stop move zero power cell
        if (component.PilotSlot.ContainedEntity != null)
            _actionBlocker.UpdateCanMove(component.PilotSlot.ContainedEntity.Value);
        // ADT-Tweak end
||||||| base
        _actionBlocker.UpdateCanMove(uid);
=======
        Vehicle.RefreshCanRun(uid);
>>>>>>> theirs

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    public void RemoveBattery(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _container.EmptyContainer(component.BatterySlot);
        component.Energy = 0;
        component.MaxEnergy = 0;

<<<<<<< ours
        _actionBlocker.UpdateCanMove(uid);
        // ADT-Tweak start: stop move zero power cell
        if (component.PilotSlot.ContainedEntity != null)
            _actionBlocker.UpdateCanMove(component.PilotSlot.ContainedEntity.Value);
        // ADT-Tweak end
||||||| base
        _actionBlocker.UpdateCanMove(uid);
=======
        Vehicle.RefreshCanRun(uid);
>>>>>>> theirs

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    #region Atmos Handling
<<<<<<< ours
    // private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args) // ADT - Moved to shared
    // {
    //     if (!TryComp<MechComponent>(component.Mech, out var mech) ||
    //         !TryComp<MechAirComponent>(component.Mech, out var mechAir))
    //     {
    //         return;
    //     }

    //     if (mech.Airtight)
    //         args.Gas = mechAir.Air;
    // }
    // ADT Commented
||||||| base
    private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech) ||
            !TryComp<MechAirComponent>(component.Mech, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }
=======
    [SubscribeLocalEvent]
    private void OnInhale(Entity<VehicleOperatorComponent> ent, ref InhaleLocationEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle ||
            !TryComp<MechComponent>(vehicle, out var mech) ||
            !TryComp<MechAirComponent>(vehicle, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }
>>>>>>> theirs

    [SubscribeLocalEvent]
    private void OnExhale(Entity<VehicleOperatorComponent> ent, ref ExhaleLocationEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicle ||
            !TryComp<MechComponent>(vehicle, out var mech) ||
            !TryComp<MechAirComponent>(vehicle, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }

    [SubscribeLocalEvent]
    private void OnExpose(Entity<VehicleOperatorComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled || ent.Comp.Vehicle is not { } vehicle)
            return;

        if (!TryComp(vehicle, out MechComponent? mech))
            return;

        if (mech.Airtight && TryComp(vehicle, out MechAirComponent? air))
        {
            args.Handled = true;
            args.Gas = mech.Airtight ? air.Air : _atmosphere.GetContainingMixture(component.Mech);  // ADT Tweak
            return;
        }

        args.Gas = _atmosphere.GetContainingMixture(vehicle, excite: args.Excite);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetFilterAir(EntityUid uid, MechAirComponent comp, ref GetFilterAirEvent args)
    {
        if (args.Air != null)
            return;

        // only airtight mechs get internal air
        if (!TryComp<MechComponent>(uid, out var mech) || !mech.Airtight)
            return;

        args.Air = comp.Air;
    }
    #endregion
}
