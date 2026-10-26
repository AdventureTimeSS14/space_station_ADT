using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Server.Mech.Systems;

public sealed class MechCockpitSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public const string SeatSlotId = "adt-mech-cockpit-slot";

    public override void Initialize()
    {
        SubscribeLocalEvent<MechCockpitComponent, MechEquipmentRemovedEvent>(OnCockpitRemoved);
        SubscribeLocalEvent<MechCockpitComponent, EntityTerminatingEvent>(OnCockpitTerminating);

        SubscribeLocalEvent<MechComponent, MechCockpitEntryEvent>(OnEntryDoAfter);
        SubscribeLocalEvent<MechComponent, MechCockpitEjectEvent>(OnEjectAction);
        SubscribeLocalEvent<MechComponent, MechFlightModeChangedEvent>(OnFlightModeChanged);
        SubscribeLocalEvent<MechComponent, SetupMechUserEvent>(OnPilotSetup);
        SubscribeLocalEvent<MechComponent, RemoveMechUserEvent>(OnPilotRemoved);

        SubscribeLocalEvent<MechCockpitPassengerComponent, EntGotRemovedFromContainerMessage>(OnPassengerExited);
    }

    private void OnCockpitRemoved(EntityUid uid, MechCockpitComponent comp, ref MechEquipmentRemovedEvent args)
    {
        RemovePassenger(args.Mech);
    }

    private void OnCockpitTerminating(EntityUid uid, MechCockpitComponent comp, ref EntityTerminatingEvent args)
    {
        if (CompOrNull<MechEquipmentComponent>(uid)?.EquipmentOwner is { } mech)
            RemovePassenger(mech);
    }

    public void AddPassengerVerb(EntityUid uid, MechComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (GetPassenger(uid) is { } current)
        {
            if (current != args.User)
                return;

            var mech = uid;
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("adt-mech-cockpit-verb-exit"),
                Priority = 2,
                Act = () => RemovePassenger(mech),
            });
            return;
        }

        if (comp.Broken)
            return;

        if (FindCockpit(uid, comp) is not { } cockpit)
            return;

        if (args.User == comp.PilotSlot.ContainedEntity)
            return;

        var whitelist = cockpit.Comp.PilotWhitelist ?? comp.PilotWhitelist;
        if (_whitelist.IsWhitelistFail(whitelist, args.User))
            return;

        if (!_actionBlocker.CanMove(args.User))
            return;

        var user = args.User;
        var delay = cockpit.Comp.EntryDelay;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("adt-mech-cockpit-verb-enter"),
            Act = () =>
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new MechCockpitEntryEvent(), uid, target: uid)
                {
                    BreakOnMove = true,
                };

                _doAfter.TryStartDoAfter(doAfterArgs);
            },
        });
    }

    private void OnEntryDoAfter(EntityUid uid, MechComponent comp, MechCockpitEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (FindCockpit(uid, comp) is not { } cockpit)
            return;

        if (GetPassenger(uid) != null)
            return;

        var seat = _container.EnsureContainer<ContainerSlot>(uid, SeatSlotId);
        if (!_container.Insert(args.User, seat))
            return;

        var passenger = EnsureComp<MechCockpitPassengerComponent>(args.User);
        passenger.Mech = uid;
        passenger.Cockpit = cockpit.Owner;
        Dirty(args.User, passenger);

        var pilotComp = EnsureComp<MechPilotComponent>(args.User);
        pilotComp.Mech = uid;
        Dirty(args.User, pilotComp);

        _actions.AddAction(args.User, ref cockpit.Comp.EjectActionEntity, cockpit.Comp.EjectAction, uid);
        _actions.AddAction(args.User, ref cockpit.Comp.UiActionEntity, cockpit.Comp.UiAction, uid);
        _actions.AddAction(args.User, ref cockpit.Comp.CycleActionEntity, cockpit.Comp.CycleAction, uid);
        Dirty(cockpit.Owner, cockpit.Comp);

        UpdateControl(uid, comp);
    }

    private void OnEjectAction(EntityUid uid, MechComponent comp, MechCockpitEjectEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        RemovePassenger(uid);
    }

    public void RemovePassenger(EntityUid mech)
    {
        if (GetPassenger(mech) is not { } passenger)
            return;

        if (_container.TryGetContainer(mech, SeatSlotId, out var seat))
        {
            _container.Remove(passenger, seat, force: true);
        }

        _transform.AttachToGridOrMap(passenger);
    }

    private void OnPassengerExited(EntityUid uid, MechCockpitPassengerComponent comp, EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != SeatSlotId)
            return;

        var mech = comp.Mech;

        _actions.RemoveProvidedActions(uid, mech);

        RemComp<MechCockpitPassengerComponent>(uid);
        RemComp<MechPilotComponent>(uid);
        RemComp<MechControlLockedComponent>(uid);
        RemComp<InteractionRelayComponent>(uid);

        UpdateControl(mech);
    }

    private void OnFlightModeChanged(EntityUid uid, MechComponent comp, ref MechFlightModeChangedEvent args)
    {
        UpdateControl(uid, comp);
    }

    private void OnPilotSetup(EntityUid uid, MechComponent comp, ref SetupMechUserEvent args)
    {
        UpdateControl(uid, comp, entering: args.Pilot);
    }

    private void OnPilotRemoved(EntityUid uid, MechComponent comp, ref RemoveMechUserEvent args)
    {
        RemComp<MechControlLockedComponent>(args.Pilot);

        UpdateControl(uid, comp, leaving: args.Pilot);
    }

    public void UpdateControl(EntityUid uid, MechComponent? comp = null, EntityUid? entering = null, EntityUid? leaving = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        var pilot = comp.PilotSlot.ContainedEntity ?? entering;
        if (pilot == leaving)
            pilot = null;

        var passenger = GetPassenger(uid);
        var flying = CompOrNull<MechThrusterComponent>(uid)?.Active == true;

        var passengerInControl = flying && passenger != null;

        if (pilot != null)
            SetControl(pilot.Value, uid, !passengerInControl);

        if (passenger != null)
            SetControl(passenger.Value, uid, passengerInControl);

        if (passengerInControl)
            EnsureComp<MechSecondPilotControlComponent>(uid);
        else
            RemComp<MechSecondPilotControlComponent>(uid);
    }

    private void SetControl(EntityUid crew, EntityUid mech, bool allowed)
    {
        if (allowed)
        {
            RemComp<MechControlLockedComponent>(crew);

            var relay = EnsureComp<InteractionRelayComponent>(crew);
            _interaction.SetRelay(crew, mech, relay);
        }
        else
        {
            EnsureComp<MechControlLockedComponent>(crew);
            RemComp<InteractionRelayComponent>(crew);
        }
    }

    public void RelayDamageToPassenger(EntityUid uid, MechComponent comp, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (GetPassenger(uid) is not { } passenger)
            return;

        _damageable.ChangeDamage(passenger, args.DamageDelta * comp.MechToPilotDamageMultiplier);
    }

    public EntityUid? GetPassenger(EntityUid mech)
    {
        if (!_container.TryGetContainer(mech, SeatSlotId, out var seat))
            return null;

        return seat.ContainedEntities.Count > 0 ? seat.ContainedEntities[0] : null;
    }

    private Entity<MechCockpitComponent>? FindCockpit(EntityUid uid, MechComponent comp)
    {
        foreach (var equipment in comp.EquipmentContainer.ContainedEntities)
        {
            if (TryComp<MechCockpitComponent>(equipment, out var cockpit))
                return (equipment, cockpit);
        }

        return null;
    }
}
