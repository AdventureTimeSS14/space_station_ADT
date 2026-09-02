using Content.Shared.Actions;
using Content.Shared.Mech.Components;
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
        SubscribeLocalEvent<MechOverloadComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<MechOverloadComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
    }

    private void OnToggleOverload(EntityUid uid, MechOverloadComponent comp, MechOverloadEvent args)
    {
        if (!TryComp<MechComponent>(uid, out var mech))
            return;
        if (mech.Integrity <= comp.MinIng)
            return;

        if (!comp.Overload)
        {
            comp.Overload = true;
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
            _movementSpeedModifierSystem.RefreshFrictionModifiers(uid);
            mech.MechEnergyWaste += 20;
            Spawn("EffectSparks", Transform(uid).Coordinates);
            _damageable.TryChangeDamage(uid, comp.DamagePerSpeed, ignoreResistances: true);
        }
        else
        {
            comp.Overload = false;
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
            _movementSpeedModifierSystem.RefreshFrictionModifiers(uid);
            mech.MechEnergyWaste -= 20;
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

        component.Overload = false;
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        _movementSpeedModifierSystem.RefreshFrictionModifiers(uid);
        mech.MechEnergyWaste -= 20;
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, MechOverloadComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!comp.Overload)
            return;

        args.ModifySpeed(comp.WalkSpeedMultiplier, comp.SprintSpeedMultiplier);
    }

    private void OnRefreshFrictionModifiers(EntityUid uid, MechOverloadComponent comp, ref RefreshFrictionModifiersEvent args)
    {
        if (!comp.Overload)
            return;

        args.ModifyAcceleration(comp.AccelerationMultiplier);
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

