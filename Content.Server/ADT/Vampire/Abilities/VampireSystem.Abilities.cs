// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.ADT.Damage;
using Content.Shared.ADT.Vampire;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Flash.Components;
using Robust.Shared.Timing;
using Content.Shared.Movement.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Content.Shared.Stunnable;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Stealth;
using Content.Server.Polymorph.Systems;
using Content.Shared.Bible.Components;
using Content.Shared.Humanoid;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private SharedFlashSystem _flash = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;

    private static readonly EntProtoId ForceSleeping = "StatusEffectForcedSleeping";
    private static readonly EntProtoId Muted = "StatusEffectMuted";

    private void InitializePowers()
    {
        InitializeHemomancer();
        InitializeUmbrae();
        InitializeGargantua();
        InitializeDantalion();
        InitializeBestia();
        InitializeVariants();

        // Basic Abilities
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateActionEvent>(OnRejuvenate);
        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnVampireGlare);
    }

    #region Basic Abilities

    private void OnRejuvenate(Entity<VampireComponent> ent, ref VampireRejuvenateActionEvent args)
    {
        if (_mobState.IsDead(args.Performer))
        {
            _popup.PopupEntity(Loc.GetString("vampire-heal-dead"), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        TryRemoveKnockdown(args.Performer);
        ResetStamina(args.Performer);

        if (args.Advanced || ent.Comp.CurrentBlood >= args.BloodCost)
        {
            ExecuteRejuvenateHealTick(args.Performer, 0, args);
        }

        args.Handled = true;
    }

    private void OnVampireGlare(Entity<VampireComponent> ent, ref VampireGlareActionEvent args)
    {
        var aoe = ent.Comp.Variant != VampireVariant.Classic
            || TryComp<VampireDiablerieComponent>(ent, out var diablerie) && diablerie.DiablerieLevel >= 4;

        if (aoe)
        {
            args.Handled = true;
            var coords = Transform(ent).Coordinates;
            foreach (var victim in _entityLookup.GetEntitiesInRange<HumanoidProfileComponent>(coords, 4f))
            {
                if (victim.Owner == ent.Owner || HasComp<ThrallComponent>(victim.Owner))
                    continue;

                TryGlareTarget(ent, victim.Owner, true);
            }
            return;
        }

        if (TryGlareTarget(ent, args.Target, false))
            args.Handled = true;
    }

    private bool TryGlareTarget(Entity<VampireComponent> ent, EntityUid target, bool aoe)
    {
        if (HasComp<VampireComponent>(target) || (HasComp<FlashImmunityComponent>(target) && !HasComp<BorgChassisComponent>(target)))
            return false;

        if (HasComp<BibleUserComponent>(target) && !HasTruePower(ent))
        {
            if (!aoe)
            {
                _stun.TryUpdateParalyzeDuration(ent.Owner, TimeSpan.FromSeconds(5f));
                _chat.TryEmoteWithoutChat(ent.Owner, ProtoMan.Index(Scream), true);
                _damage.TryChangeDamage(ent.Owner, ent.Comp.HolyDamage);
            }
            return false;
        }

        var ev = new FlashAttemptEvent(target, ent.Owner, null);
        RaiseLocalEvent(target, ref ev, true);
        if (ev.Cancelled && (!TryComp<VampireDiablerieComponent>(ent, out var dieal) || dieal.DiablerieLevel < 2))
            return false;

        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(5f));
        _flash.Flash(target, ent.Owner, null, TimeSpan.FromSeconds(3f), 0.8f);
        _status.TryAddStatusEffectDuration(target, Muted, TimeSpan.FromSeconds(8f));
        return true;
    }

    #endregion

    #region Utility Methods

    private void SendFailedPopup(EntityUid uid)
    {
        _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
    }

    private void ResetStamina(EntityUid uid)
    {
        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        _stamina.ExitStamCrit(uid, stamina);
        _stamina.TakeStaminaDamage(uid, -stamina.StaminaDamage, stamina, visual: false, ignoreResist: true);
    }

    private bool TryRemoveKnockdown(Entity<StaminaComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        _status.TryRemoveStatusEffect(entity.Owner, SharedStunSystem.StunId);
        _stun.ForceStandUp(entity.Owner);
        return true;
    }

    private void ExecuteRejuvenateHealTick(EntityUid uid, int currentTick, VampireRejuvenateActionEvent args)
    {
        if (!Exists(uid) || currentTick >= args.Repeats)
            return;
		
        if (args.Advanced || !args.NoHeal)
        {
            var multiplier = args.Advanced ? GetScaledHealModifier(uid) : 1f;
            var stomachBonus = GetOrganTypeCount(uid, BestiaOrganType.Stomach) * 3;
            multiplier *= 1f + stomachBonus / 100f;

            _groupHeal.Heal(uid, args.Heal, args.HealGroups, multiplier, origin: uid);
        }

        Timer.Spawn(args.TimeInterval, () => ExecuteRejuvenateHealTick(uid, currentTick + 1, args));
    }

    private float GetScaledHealModifier(EntityUid uid)
    {
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return 0f;

        var totalDamage = _damage.GetTotalDamage(uid).Float();
        var maxDamage = threshold.Value.Float();
        var damagePercent = maxDamage > 0 ? totalDamage / maxDamage * 100 : 0;
        return Math.Clamp(damagePercent / 20f, 1f, 5f);
    }

    private bool TrySpawnObjectAtPosition(EntityCoordinates coords, EntProtoId entityId, EntityUid uid)
    {
        var grid = _transform.GetGrid(coords);
        if (grid == null) return false;

        var gridEntityUid = grid.Value;
        if (!TryComp<MapGridComponent>(gridEntityUid, out var gridComp))
            return false;

        if (!_map.TryGetTileRef(gridEntityUid, gridComp, coords, out var tileRef)
            || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            return false;

        var ent = Spawn(entityId, coords);

        var comp = EnsureComp<PreventCollideComponent>(ent);
        comp.Uid = uid;

        return true;
    }

    #endregion
}
