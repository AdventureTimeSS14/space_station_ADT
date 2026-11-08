using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Server.GameObjects;
using Content.Server.Emp;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Emp;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Random;

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class MechSystem
{
    [Dependency] private SharedMechSystem _mech = default!;
    [Dependency] private MechCockpitSystem _cockpit = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<MechComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<MechComponent, MechEquipmentDestroyedEvent>(OnEquipmentDestroyed);
        SubscribeLocalEvent<MechComponent, MechTurnLightsEvent>(OnTurnLightsEvent);
        SubscribeLocalEvent<MechComponent, MechInhaleEvent>(OnToggleInhale);
        SubscribeLocalEvent<MechComponent, ContainerVehicleEntryEvent>(OnMechEntry, before: new[] { typeof(VehicleSystem) });
    }

    protected override void AddAlternativeVerbsADT(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        _cockpit.AddPassengerVerb(uid, component, args);
    }

    private void OnMechEntry(EntityUid uid, MechComponent component, ContainerVehicleEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<AccessReaderComponent>(uid, out var access) || _accessReader.IsAllowed(args.User, uid, access))
            return;

        _popup.PopupEntity(Loc.GetString("gateway-access-denied"), args.User, args.User);
        _audio.PlayPvs(component.AccessDeniedSound, uid);
        args.Handled = true;
    }

    private void OnDamageChangedADT(EntityUid uid, MechComponent component, DamageChangedEvent args)
    {
        if (component.Integrity <= component.DamageToDesEqi && !component.Broken && _random.Prob(0.5f) && component.CurrentSelectedEquipment != null)
        {
            var ev = new MechEquipmentDestroyedEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        if (args.DamageIncreased && args.DamageDelta != null && GetPilot(uid) is { } pilot)
            _damageable.ChangeDamage(pilot, args.DamageDelta * component.MechToPilotDamageMultiplier);

        _cockpit.RelayDamageToPassenger(uid, component, args);
    }

    private void OnToggleInhale(EntityUid uid, MechComponent component, MechInhaleEvent args)
    {
        if (component.Airtight)
        {
            component.Airtight = false;
            return;
        }
        component.Airtight = true;
    }

    private void OnDamageModify(EntityUid uid, MechComponent component, DamageModifyEvent args)
    {
        if (component.Modifiers != null)
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.Modifiers);
    }

    private void OnTurnLightsEvent(EntityUid uid, MechComponent component, MechTurnLightsEvent args)
    {
        if (HasComp<PointLightComponent>(uid))
        {
            RemComp<PointLightComponent>(uid);
            _audio.PlayPvs(component.MechLightsOffSound, uid);
        }
        else
        {
            AddComp<PointLightComponent>(uid);
            _audio.PlayPvs(component.MechLightsOnSound, uid);
        }
    }

    private void OnEquipmentDestroyed(EntityUid uid, MechComponent component, ref MechEquipmentDestroyedEvent args)
    {
        if (!component.CurrentSelectedEquipment.HasValue)
            return;

        Spawn("EffectSparks", Transform(uid).Coordinates);
        _audio.PlayPvs(component.EquipmentDestroyedSound, uid);

        var equipment = component.CurrentSelectedEquipment.Value;
        _mech.RemoveEquipment(uid, equipment, component, forced: true);

        QueueDel(equipment);
    }

    private void OnEmpPulse(EntityUid uid, MechComponent comp, ref EmpPulseEvent args)
    {
        var damage = args.EnergyConsumption / 100;
        TryChangeEnergy(uid, -FixedPoint2.Min(comp.Energy, damage), comp);
        Spawn("EffectEmpPulse", Transform(uid).Coordinates);
    }

    public override void UpdateUserInterfaceByEquipment(EntityUid equipmentUid)
    {
        base.UpdateUserInterfaceByEquipment(equipmentUid);

        if (!TryComp<MechEquipmentComponent>(equipmentUid, out var comp))
        {
            Log.Error("Could not find mech equipment owner to update UI.");
            return;
        }
        if (!comp.EquipmentOwner.HasValue)
            return;
        UpdateUserInterface(comp.EquipmentOwner.Value);
    }
}
