// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Damage.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
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

namespace Content.Server.Vampire;

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
        _stamina.RemoveStaminaDamage(args.Performer);

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
		
		var healingSpec = new DamageSpecifier();

		if (args.Advanced)
		{
			healingSpec = CalculateScaledHealing(uid, args.Heal, args.HealGroups);
		}
		else if (!args.NoHeal)
		{
			healingSpec = SimpleHealing(uid, args.Heal, args.HealGroups);
		}

        var stomachCount = GetOrganTypeCount(uid, BestiaOrganType.Stomach);
        var bonus = stomachCount * 3;
        if (bonus > 0)
        {
            var bonusMultiplier = 1f + (bonus / 100f);
            healingSpec *= bonusMultiplier;
        }

        _damage.TryChangeDamage(uid, healingSpec, true, false, origin: uid);

        Timer.Spawn(args.TimeInterval, () => ExecuteRejuvenateHealTick(uid, currentTick + 1, args));
    }

    private DamageSpecifier CalculateScaledHealing(EntityUid uid, DamageSpecifier heal, GroupHealSpecifier healGroups)
    {
        var totalDamage = _damage.GetTotalDamage(uid).Float();
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return new DamageSpecifier();

        var maxDamage = threshold.Value.Float();
        var damagePercent = maxDamage > 0 ? (totalDamage / maxDamage) * 100 : 0;
        var modifier = Math.Clamp(damagePercent / 20.0, 1.0, 5.0);

        var groupHealSpec = _damage.CreateWeightedHealFromGroups(uid, healGroups);
        var scaledHeal = (heal + groupHealSpec) * modifier;

        return scaledHeal;
    }

    private DamageSpecifier SimpleHealing(EntityUid uid, DamageSpecifier heal, GroupHealSpecifier healGroups)
    {
        var groupHealSpec = _damage.CreateWeightedHealFromGroups(uid, healGroups);
        var scaledHeal = (heal + groupHealSpec);

        return scaledHeal;
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
