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
using Content.Shared.ADT.EMP;
using Content.Server.ADT.Mech.Components;
using Content.Shared.ADT.Emp;

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class MechSystem
{
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly MechCockpitSystem _cockpit = default!;

    private void InitializeADT()
    {
        SubscribeLocalEvent<MechEmpGlitchComponent, SetupMechUserEvent>(OnSetupGlitch);
        SubscribeLocalEvent<MechEmpGlitchComponent, RemoveMechUserEvent>(OnRemoveGlitch);

        SubscribeLocalEvent<MechComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<MechComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<MechComponent, MechEquipmentDestroyedEvent>(OnEquipmentDestroyed);
        SubscribeLocalEvent<MechComponent, MechTurnLightsEvent>(OnTurnLightsEvent);
        SubscribeLocalEvent<MechComponent, MechInhaleEvent>(OnToggleInhale);
    }

    private void OnSetupGlitch(Entity<MechEmpGlitchComponent> ent, ref SetupMechUserEvent args)
    {
        if (TryComp<EmpGlitchComponent>(args.Pilot, out var glitch))
        {
            ent.Comp.CachedCameraGlitchDurationMultiplier = glitch.DurationMultiplier;
            ent.Comp.CachedCameraGlitchMotionStrength = glitch.MotionStrength;
            ent.Comp.CachedCameraGlitchSmear = glitch.Smear;
            ent.Comp.CachedCameraGlitchAmount = glitch.Glitch;
            ent.Comp.HadCameraGlitch = true;
        }

        glitch = EnsureComp<EmpGlitchComponent>(args.Pilot);
        glitch.DurationMultiplier = ent.Comp.CameraGlitchDurationMultiplier;
        glitch.MotionStrength = ent.Comp.CameraGlitchMotionStrength;
        glitch.Smear = ent.Comp.CameraGlitchSmear;
        glitch.Glitch = ent.Comp.CameraGlitchAmount;
    }

    private void OnRemoveGlitch(Entity<MechEmpGlitchComponent> ent, ref RemoveMechUserEvent args)
    {
        if (ent.Comp.HadCameraGlitch)
        {
            var glitch = EnsureComp<EmpGlitchComponent>(args.Pilot);
            glitch.DurationMultiplier = ent.Comp.CachedCameraGlitchDurationMultiplier;
            glitch.MotionStrength = ent.Comp.CachedCameraGlitchMotionStrength;
            glitch.Smear = ent.Comp.CachedCameraGlitchSmear;
            glitch.Glitch = ent.Comp.CachedCameraGlitchAmount;
        }
        else
        {
            RemComp<EmpGlitchComponent>(args.Pilot);
        }

        ent.Comp.HadCameraGlitch = false;
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

        if (TryComp<EmpPowerDrainCapComponent>(uid, out var cap))
        {
            damage = MathF.Min(damage, cap.RelativeToMaxCharge ? (float)comp.MaxEnergy : (float)comp.Energy) * cap.CapPercentage;
        }

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
