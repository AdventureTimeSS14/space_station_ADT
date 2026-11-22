// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Numerics;
using Content.Server.ADT.Stunnable.Components;
using Content.Shared.ADT.Interaction.Components;
using Content.Shared.ADT.Vampire;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Body;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private SharedEnsnareableSystem _ensnareable = default!;

    public static readonly ProtoId<DamageModifierSetPrototype> BloodSwell = "VampireBloodSwell";
    private static readonly TimeSpan BloodSwellStunInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BloodSwellStunReduction = TimeSpan.FromSeconds(3);
    private const float BloodSwellStaminaCoefficient = 0.5f;

    private void InitializeGargantua()
    {
        SubscribeLocalEvent<VampireComponent, VampireBloodSwellActionEvent>(OnBloodSwell);
        SubscribeLocalEvent<VampireComponent, VampireBloodRushActionEvent>(OnBloodRush);
        SubscribeLocalEvent<VampireComponent, VampireSeismicStompActionEvent>(OnSeismicStomp);
        SubscribeLocalEvent<VampireComponent, VampireOverwhelmingForceActionEvent>(OnOverwhelmingForce);
        SubscribeLocalEvent<VampireComponent, VampireDemonicGraspActionEvent>(OnDemonicGrasp);
        SubscribeLocalEvent<VampireComponent, VampireChargeActionEvent>(OnCharge);
        SubscribeLocalEvent<StunOtherOnHitComponent, ThrowDoHitEvent>(OnStunOtherOnHit);
    }

    private void OnBloodSwell(Entity<VampireComponent> ent, ref VampireBloodSwellActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        EnsureComp<BlockCombatModeInteractComponent>(ent.Owner);
        ReduceBloodSwellStuns(ent.Owner, args.Time);

        if (args.Advanced)
            ApplyAdvancedBloodSwell(ent, args.Time);

        if (TryComp(ent, out MeleeWeaponComponent? meleeWeapon))
        {
            var oldAutoAttack = meleeWeapon.AutoAttack;
            var oldAttackRate = meleeWeapon.AttackRate;
            var damageType = args.BonusDamageType;
            var hadDamage = meleeWeapon.Damage.DamageDict.TryGetValue(damageType, out var oldDamage);

            meleeWeapon.AutoAttack = true;
            meleeWeapon.AttackRate = args.AttackRate;
            meleeWeapon.Damage.DamageDict[damageType] = oldDamage + args.BonusDamageAmount;
            Dirty(ent.Owner, meleeWeapon);

            Timer.Spawn(args.Time, () =>
            {
                if (Deleted(ent.Owner))
                    return;

                if (TryComp(ent, out MeleeWeaponComponent? weapon))
                {
                    if (hadDamage)
                        weapon.Damage.DamageDict[damageType] = oldDamage;
                    else
                        weapon.Damage.DamageDict.Remove(damageType);

                    weapon.AutoAttack = oldAutoAttack;
                    weapon.AttackRate = oldAttackRate;
                    Dirty(ent.Owner, weapon);
                }

                RemComp<BlockCombatModeInteractComponent>(ent.Owner);
                _popup.PopupEntity(Loc.GetString("vampire-blood-swell-end"), ent, ent, PopupType.Medium);
            });
        }
        else
        {
            Timer.Spawn(args.Time, () =>
            {
                if (Deleted(ent.Owner))
                    return;

                RemComp<BlockCombatModeInteractComponent>(ent.Owner);
                _popup.PopupEntity(Loc.GetString("vampire-blood-swell-end"), ent, ent, PopupType.Medium);
            });
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void ApplyAdvancedBloodSwell(Entity<VampireComponent> ent, TimeSpan duration)
    {
        var hadResistance = TryComp<StaminaResistanceComponent>(ent, out var resistance);
        var oldCoefficient = resistance?.DamageCoefficient ?? 1f;
        var oldWorn = resistance?.Worn ?? false;

        resistance ??= EnsureComp<StaminaResistanceComponent>(ent);
        resistance.DamageCoefficient = BloodSwellStaminaCoefficient;
        resistance.Worn = false;
        Dirty(ent.Owner, resistance);

        _damage.SetDamageModifierSetId(ent.Owner, BloodSwell);

        Timer.Spawn(duration, () =>
        {
            if (Deleted(ent.Owner))
                return;

            _damage.SetDamageModifierSetId(ent.Owner, VampireComponent.VampireDamageModifier);

            if (!TryComp<StaminaResistanceComponent>(ent, out var current))
                return;

            if (!hadResistance)
            {
                RemComp<StaminaResistanceComponent>(ent);
                return;
            }

            current.DamageCoefficient = oldCoefficient;
            current.Worn = oldWorn;
            Dirty(ent.Owner, current);
        });
    }

    private void ReduceBloodSwellStuns(EntityUid uid, TimeSpan remaining)
    {
        if (Deleted(uid) || remaining <= TimeSpan.Zero)
            return;

        _status.TryRemoveTime(uid, SharedStunSystem.StunId, BloodSwellStunReduction);
        _stun.AddKnockdownTime(uid, -BloodSwellStunReduction);

        Timer.Spawn(BloodSwellStunInterval, () => ReduceBloodSwellStuns(uid, remaining - BloodSwellStunInterval));
    }

    private void OnStunOtherOnHit(Entity<StunOtherOnHitComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        _stun.TryAddStunDuration(args.Target, ent.Comp.Duration, visualized: true);
    }

    private void OnBloodRush(Entity<VampireComponent> ent, ref VampireBloodRushActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (TryComp(ent, out MovementSpeedModifierComponent? speedmodComponent))
        {
            var originalWalkSpeed = speedmodComponent.BaseWalkSpeed;
            var originalSprintSpeed = speedmodComponent.BaseSprintSpeed;

            _speed.ChangeBaseSpeed(ent, originalWalkSpeed * 2, originalSprintSpeed * 2, speedmodComponent.Acceleration);

            var time = HasTruePower(ent) ? args.Time * 2 : args.Time;
            Timer.Spawn(time, () =>
            {
                _speed.ChangeBaseSpeed(ent, originalWalkSpeed, originalSprintSpeed, speedmodComponent.Acceleration);
            });
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnSeismicStomp(Entity<VampireComponent> ent, ref VampireSeismicStompActionEvent args)
    {
        if (_ensnareable.IsEnsnared(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), ent, ent, PopupType.Medium);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var vampirePos = _transform.GetWorldPosition(ent);

        var gridUid = _transform.GetGrid(ent.Owner);
        if (gridUid != null && TryComp<MapGridComponent>(gridUid.Value, out var grid))
        {
            var tiles = _map.GetTilesIntersecting(gridUid.Value, grid,
                Box2.CenteredAround(vampirePos, new Vector2(6, 6)), ignoreEmpty: true);

            foreach (var tile in tiles)
            {
                if (!_random.Prob(0.5f))
                    continue;

                _tile.PryTile(tile);
            }
        }

        var nearbyHumanoids = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(ent).Coordinates, 3f);
        foreach (var humanoid in nearbyHumanoids)
        {
            var humanoidUid = humanoid.Owner;
            if (humanoidUid == ent.Owner) continue;

            if (IsProtectedByFaith(humanoidUid, ent))
                continue;

            if (!TryComp(humanoid, out PhysicsComponent? physics))
                continue;

            var humanoidPosition = _transform.GetWorldPosition(humanoid);
            var direction = (humanoidPosition - vampirePos).Normalized();

            var force = 10f;
            if (physics.Mass <= 50f)
                force *= 2;

            _throwing.TryThrow(humanoidUid, direction * (force / 2), force);
			
			_stun.TryKnockdown(humanoidUid, TimeSpan.FromSeconds(2f), true, true, false);
        }

        _audio.PlayPvs(args.Sound, ent);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnOverwhelmingForce(Entity<VampireComponent> ent, ref VampireOverwhelmingForceActionEvent args)
    {
        if (_ensnareable.IsEnsnared(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), ent, ent, PopupType.Medium);
            return;
        }

        bool prying = HasComp<PryingComponent>(ent);
        if (!prying && HasComp<PullableComponent>(ent))
        {
            if (!CheckBloodEssence(ent.Owner, args.BloodCost))
            {
                SendFailedPopup(ent);
                return;
            }
        }

        if (prying)
        {
            RemComp<PryingComponent>(ent);
            EnsureComp<EnsnareableComponent>(ent);
            EnsureComp<PullableComponent>(ent);
        }
        else
        {
            var pryComponent = EnsureComp<PryingComponent>(ent);

            pryComponent.PryPowered = true;
            pryComponent.Force = true;
            pryComponent.SpeedModifier = 2.5f;

            SubtractBloodEssence(ent.Owner, args.BloodCost);

            RemComp<EnsnareableComponent>(ent);
            RemComp<PullableComponent>(ent);
        }

        args.Handled = true;
    }

    private void OnDemonicGrasp(Entity<VampireComponent> ent, ref VampireDemonicGraspActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(ent, out var combatMode))
            return;

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var target = args.Target;
        if (IsProtectedByFaith(target, ent))
            return;

        var vampirePosition = _transform.GetWorldPosition(ent);
        var targetPosition = _transform.GetWorldPosition(target);
        var direction = (vampirePosition - targetPosition).Normalized();

        if (HasComp<PhysicsComponent>(target))
        {
            if (!combatMode.IsInCombatMode)
            {
                _throwing.TryThrow(target, -direction * 3);
                _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(3f));
            }
            else
            {
                _throwing.TryThrow(target, direction * 3);
                _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(3f));
            }
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnCharge(Entity<VampireComponent> ent, ref VampireChargeActionEvent args)
    {
        if (_ensnareable.IsEnsnared(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), ent, ent, PopupType.Medium);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var coords = args.Target;
        var vampirePosition = _transform.GetWorldPosition(ent);
        var targetPosition = _transform.ToMapCoordinates(coords, true).Position;
        var direction = (targetPosition - vampirePosition).Normalized();

        // Well, that might cause it to deal damage when the space wind,
        // but that doesn't seem like a problem, does it?
        EntityManager.AddComponents(ent, args.EnsurableComponents, false);

        _throwing.TryThrow(ent, direction * 3);

        _audio.PlayPvs(args.Sound, ent);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }
}
