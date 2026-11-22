using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Mech.Components;
using Content.Server.Mech.Systems;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.ADT.Mech.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.FixedPoint;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mech.Systems;

public sealed class ADTMechSystem : SharedADTMechSystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MechCockpitSystem _cockpit = default!;
    [Dependency] private MechSystem _mech = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private VehicleSystem _vehicle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTMechComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ADTMechComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ADTMechComponent, MechSealToggledEvent>(OnSealToggled);
        SubscribeLocalEvent<ADTMechComponent, RepairedEvent>(OnRepaired);

        SubscribeLocalEvent<MechEquipmentDurabilityComponent, DamageChangedEvent>(OnEquipmentDamageChanged);
    }

    protected override void AddExtraVerbs(Entity<ADTMechComponent> ent, MechComponent mech, GetVerbsEvent<AlternativeVerb> args)
    {
        _cockpit.AddPassengerVerb(ent, mech, args);
    }

    private void OnDamageChanged(Entity<ADTMechComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var delta = DamageSpecifier.GetPositive(args.DamageDelta);
        if (delta.Empty)
            return;

        if (ent.Comp.PilotDamageMultiplier > 0f && _vehicle.GetOperatorOrNull(ent.Owner) is { } pilot)
            _damageable.ChangeDamage(pilot, delta * ent.Comp.PilotDamageMultiplier);

        _cockpit.RelayDamageToPassenger(ent, ent.Comp.PilotDamageMultiplier, delta);

        DamageRandomEquipment(ent, delta);
    }

    private void DamageRandomEquipment(Entity<ADTMechComponent> ent, DamageSpecifier delta)
    {
        if (ent.Comp.EquipmentDamageShare <= 0f || !TryComp<MechComponent>(ent, out var mech))
            return;

        var candidates = mech.EquipmentContainer.ContainedEntities
            .Where(e => TryComp<MechEquipmentDurabilityComponent>(e, out var durability) && !durability.Broken)
            .ToList();

        if (candidates.Count == 0)
            return;

        var target = _random.Pick(candidates);
        _damageable.ChangeDamage(target, delta * ent.Comp.EquipmentDamageShare, ignoreResistances: true);
    }

    private void OnEquipmentDamageChanged(Entity<MechEquipmentDurabilityComponent> ent, ref DamageChangedEvent args)
    {
        var broken = _damageable.GetTotalDamage((ent.Owner, args.Damageable)) >= ent.Comp.MaxDurability;
        if (broken == ent.Comp.Broken)
            return;

        ent.Comp.Broken = broken;
        Dirty(ent);

        if (!broken)
            return;

        if (CompOrNull<MechEquipmentComponent>(ent)?.EquipmentOwner is not { } mech)
            return;

        ForceDeselect(mech, ent);

        Spawn("EffectSparks", Transform(mech).Coordinates);

        if (TryComp<ADTMechComponent>(mech, out var adtMech))
            _audio.PlayPvs(adtMech.EquipmentBrokenSound, mech);

        if (_vehicle.GetOperatorOrNull(mech) is { } pilot)
            _popup.PopupEntity(Loc.GetString("adt-mech-equipment-broken", ("item", ent.Owner)), mech, pilot, PopupType.MediumCaution);

        _mech.UpdateUserInterface(mech);
    }

    private void OnDamageModify(Entity<ADTMechComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Modifiers != null)
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.Modifiers);
    }

    private void OnEmpPulse(Entity<ADTMechComponent> ent, ref EmpPulseEvent args)
    {
        if (!TryComp<MechComponent>(ent, out var mech))
            return;

        var drain = FixedPoint2.Min(mech.Energy, args.EnergyConsumption / 100);
        _mech.TryChangeEnergy(ent, -drain, mech);
        Spawn("EffectEmpPulse", Transform(ent).Coordinates);
    }

    private void OnSealToggled(Entity<ADTMechComponent> ent, ref MechSealToggledEvent args)
    {
        if (!TryComp<MechAirComponent>(ent, out var air))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, ignoreExposed: true, excite: true);

        if (!args.Sealed)
        {
            var released = air.Air.RemoveRatio(1f);
            if (environment != null)
                _atmosphere.Merge(environment, released);

            return;
        }

        if (environment == null || environment.Pressure <= air.Air.Pressure)
            return;

        var moles = _atmosphere.MolesToEqualizePressure(environment, air.Air);
        _atmosphere.Merge(air.Air, environment.Remove(moles));
    }

    private void OnRepaired(Entity<ADTMechComponent> ent, ref RepairedEvent args)
    {
        if (!TryComp<MechComponent>(ent, out var mech))
            return;

        foreach (var equipment in mech.EquipmentContainer.ContainedEntities)
        {
            if (HasComp<MechEquipmentDurabilityComponent>(equipment))
                _damageable.ClearAllDamage(equipment);
        }

        _mech.UpdateUserInterface(ent, mech);
    }
}
