using Content.Shared.Actions;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.ADT.Mech.Components;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// Handles all of the interactions, UI handling, and items shennanigans for <see cref="MechComponent"/>
/// </summary>
public sealed class MechOverloadSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechOverloadComponent, MechOverloadEvent>(OnToggleOverload);
        SubscribeLocalEvent<MechOverloadComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnToggleOverload(EntityUid uid, MechOverloadComponent comp, MechOverloadEvent args)
    {
        if (!TryComp<MechComponent>(uid, out var mech))
            return;
        if (mech.Integrity <= comp.MinIng)
            return;

        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);

        if (comp.Overload == false)
        {
            comp.SavedWalkSpeed = movementSpeed.BaseWalkSpeed;
            comp.SavedSprintSpeed = movementSpeed.BaseSprintSpeed;
            comp.SavedAcceleration = movementSpeed.Acceleration;

            _movementSpeedModifierSystem.ChangeBaseSpeed(uid, 5, 5, 40, movementSpeed);
            mech.MechEnergyWaste += 20;
            comp.Overload = true;
            Spawn("EffectSparks", Transform(uid).Coordinates);
            _damageable.TryChangeDamage(uid, comp.DamagePerSpeed, ignoreResistances: true);
        }
        else
        {
            _movementSpeedModifierSystem.ChangeBaseSpeed(uid, comp.SavedWalkSpeed, comp.SavedSprintSpeed, comp.SavedAcceleration, movementSpeed);
            mech.MechEnergyWaste -= 20;
            comp.Overload = false;
        }
    }
    private void OnDamage(EntityUid uid, MechOverloadComponent component, DamageChangedEvent args)
    {
        if (!TryComp<MechComponent>(uid, out var mech))
            return;
        if (mech.Integrity > component.MinIng)
            return;
        if (!component.Overload)
            return;

        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, component.SavedWalkSpeed, component.SavedSprintSpeed, component.SavedAcceleration, movementSpeed);
        mech.MechEnergyWaste -= 20;
        component.Overload = false;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechOverloadComponent, MechComponent>();
        while (query.MoveNext(out var uid, out var overload, out var mech))
        {
            if (!overload.Overload)
                continue;
            overload.Accumulator += frameTime;
            if (overload.Accumulator < 1f)
                continue;
            overload.Accumulator = 0f;

            var dmg = mech.MechToPilotDamageMultiplier;
            mech.MechToPilotDamageMultiplier = 0f;
            _damageable.TryChangeDamage(uid, overload.DamagePerSpeed, ignoreResistances: true);
            mech.MechToPilotDamageMultiplier = dmg;
        }
    }
}

public sealed partial class MechOverloadEvent : InstantActionEvent
{
}

