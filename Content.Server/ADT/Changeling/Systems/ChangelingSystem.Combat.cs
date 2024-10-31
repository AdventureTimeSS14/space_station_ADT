using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Server.Destructible;
using Content.Shared.Movement.Systems;
using Content.Shared.ADT.Damage.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Damage.Components;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    private void InitializeCombatAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, LingEMPActionEvent>(OnLingEmp);
        SubscribeLocalEvent<ChangelingComponent, LingResonantShriekEvent>(OnResonantShriek);
        SubscribeLocalEvent<ChangelingComponent, ChangelingMusclesActionEvent>(OnMuscles);
        SubscribeLocalEvent<ChangelingComponent, BlindStingEvent>(OnBlindSting);
        SubscribeLocalEvent<ChangelingComponent, AdrenalineActionEvent>(OnAdrenaline);

        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
        SubscribeLocalEvent<ChangelingComponent, ArmShieldActionEvent>(OnArmShieldAction);
        SubscribeLocalEvent<ChangelingComponent, ArmaceActionEvent>(OnArmaceAction);
        SubscribeLocalEvent<ChangelingComponent, LingArmorActionEvent>(OnLingArmorAction);

        SubscribeLocalEvent<ChangelingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<ChangelingComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<ChangelingComponent, BeforeStaminaCritEvent>(OnBeforeStaminacrit);
    }

    #region Actions
    private void OnLingEmp(EntityUid uid, ChangelingComponent component, LingEMPActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        var coords = _transform.GetMapCoordinates(uid);
        _emp.EmpPulse(coords, component.DissonantShriekEmpRange, component.DissonantShriekEmpConsumption, component.DissonantShriekEmpDuration);
    }

    public void OnResonantShriek(EntityUid uid, ChangelingComponent component, LingResonantShriekEvent args)
    {
        if (args.Handled)
            return;

        if (_mobState.IsDead(uid))
        {
            var selfMessage = Loc.GetString("changeling-regenerate-fail-dead");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        var xform = Transform(uid);
        foreach (var ent in _lookup.GetEntitiesInRange<StatusEffectsComponent>(xform.Coordinates, 15))
        {
            if (HasComp<ChangelingComponent>(ent))
                continue;

            _flashSystem.Flash(ent, uid, null, 6f, 0.8f, false);

            if (!_mindSystem.TryGetMind(ent, out var mindId, out var mind))
                continue;
            if (mind.Session == null)
                continue;
            _audioSystem.PlayGlobal(component.SoundResonant, mind.Session);
        }
    }

    private void OnMuscles(EntityUid uid, ChangelingComponent component, ChangelingMusclesActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty))
            return;

        args.Handled = true;

        var message = Loc.GetString("changeling-lingmuscles");
        _popup.PopupEntity(message, uid, uid);

        component.MusclesActive = !component.MusclesActive;
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnAdrenaline(EntityUid uid, ChangelingComponent component, AdrenalineActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-fail-lesser-form"), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTen))
            return;

        args.Handled = true;

        _status.TryAddStatusEffect<IgnoreSlowOnDamageComponent>(uid, "Adrenaline", TimeSpan.FromMinutes(1), false);
        var selfMessage = Loc.GetString("changeling-adrenaline-self-success");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
    }

    private void OnBlindSting(EntityUid uid, ChangelingComponent component, BlindStingEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryStingTarget(uid, target, component))
            return;

        if (!TryUseAbility(uid, component, component.ChemicalsCostFifteen))
            return;

        args.Handled = true;

        _status.TryAddStatusEffect<TemporaryBlindnessComponent>(target, TemporaryBlindnessSystem.BlindingStatusEffect, component.BlindStingDuration, true);

        var selfMessageSuccess = Loc.GetString("changeling-success-sting", ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(selfMessageSuccess, uid, uid);

    }

    private void OnArmBladeAction(EntityUid uid, ChangelingComponent component, ArmBladeActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.ArmBladeActive))
            return;

        args.Handled = true;

        component.ArmBladeActive = !component.ArmBladeActive;

        if (component.ArmBladeActive)
        {
            if (!SpawnArmBlade(uid, component))
            {
                _popup.PopupEntity(Loc.GetString("changeling-armblade-fail"), uid, uid);
                return;
            }

            _audioSystem.PlayPvs(component.SoundFlesh, uid);

            var othersMessage = Loc.GetString("changeling-armblade-success-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armblade-success-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
            RemoveBladeEntity(uid, component);
    }

    private void OnArmShieldAction(EntityUid uid, ChangelingComponent component, ArmShieldActionEvent args)     // При нажатии на действие орг. щита
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.ArmShieldActive))
            return;

        args.Handled = true;

        component.ArmShieldActive = !component.ArmShieldActive;

        if (component.ArmShieldActive)
        {
            if (SpawnArmShield(uid, component))
            {
                _popup.PopupEntity(Loc.GetString("changeling-armshield-fail"), uid, uid);
                return;
            }

            _audioSystem.PlayPvs(component.SoundFlesh, uid);

            var othersMessage = Loc.GetString("changeling-armshield-success-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armshield-success-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
            RemoveShieldEntity(uid, component);
    }

    private void OnArmaceAction(EntityUid uid, ChangelingComponent component, ArmaceActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.ArmBladeActive))
            return;

        args.Handled = true;

        component.ArmaceActive = !component.ArmaceActive;

        if (component.ArmaceActive)
        {
            if (!SpawnArmace(uid, component))
            {
                _popup.PopupEntity(Loc.GetString("changeling-armace-fail"), uid, uid);
                return;
            }

            _audioSystem.PlayPvs(component.SoundFlesh, uid);

            var othersMessage = Loc.GetString("changeling-armace-success-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armace-success-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
            RemoveArmaceEntity(uid, component);
    }

    private void OnLingArmorAction(EntityUid uid, ChangelingComponent component, LingArmorActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<InventoryComponent>(uid, out var inventory))
            return;

        if (component.LesserFormActive)
        {
            var selfMessage = Loc.GetString("changeling-transform-fail-lesser-form");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!TryUseAbility(uid, component, component.ChemicalsCostTwenty, !component.LingArmorActive, component.LingArmorRegenCost))
            return;

        _audioSystem.PlayPvs(component.SoundFlesh, uid);
        component.LingArmorActive = !component.LingArmorActive;

        if (component.LingArmorActive)
        {
            args.Handled = true;

            SpawnLingArmor(uid, inventory);

            var othersMessage = Loc.GetString("changeling-armor-success-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armor-success-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
        else
        {
            _inventorySystem.TryUnequip(uid, "head", true, true, false);
            _inventorySystem.TryUnequip(uid, "outerClothing", true, true, false);

            var othersMessage = Loc.GetString("changeling-armor-retract-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armor-retract-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

            var solution = new Solution();
            solution.AddReagent("Blood", FixedPoint2.New(75));
            _puddle.TrySpillAt(Transform(uid).Coordinates, solution, out _);
        }
    }
    #endregion

    #region Events
    private void OnRefreshMovespeed(EntityUid uid, ChangelingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.MusclesActive)
            args.ModifySpeed(component.MusclesModifier);
    }

    private void OnDamage(EntityUid uid, ChangelingComponent component, DamageChangedEvent args)
    {
        if (!component.ShieldEntity.HasValue)
            return;
        if (!TryComp<DamageableComponent>(component.ShieldEntity, out var damage))
            return;

        var additionalShieldHealth = 50 * component.AbsorbedDnaModifier;
        var shieldHealth = 150 + additionalShieldHealth;
        if (damage.TotalDamage >= shieldHealth)
        {
            component.ArmShieldActive = false;
            QueueDel(component.ShieldEntity.Value);

            _audioSystem.PlayPvs(component.SoundFlesh, uid);

            var othersMessage = Loc.GetString("changeling-armshield-broke-others", ("user", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-armshield-broke-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }
    }

    private void OnBeforeStaminacrit(EntityUid uid, ChangelingComponent component, ref BeforeStaminaCritEvent args)
    {
        if (!component.MusclesActive)
            return;

        args.Cancelled = true;

        if (_timing.CurTime < component.NextMusclesDamage)
            return;

        _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), 1.5));
        component.NextMusclesDamage = _timing.CurTime + TimeSpan.FromSeconds(1);
    }
    #endregion

    #region Misc
    public bool SpawnArmBlade(EntityUid uid, ChangelingComponent component)
    {
        var armblade = Spawn("ADTArmBlade", Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(armblade);
        RemComp<DestructibleComponent>(armblade);
        if (!_handsSystem.TryPickupAnyHand(uid, armblade))
        {
            QueueDel(armblade);
            return false;
        }

        component.BladeEntity = armblade;
        return true;
    }

    public bool SpawnArmShield(EntityUid uid, ChangelingComponent component)
    {
        var armshield = Spawn("ADTArmShield", Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(armshield);

        if (!_handsSystem.TryPickupAnyHand(uid, armshield))
        {
            QueueDel(armshield);
            return false;
        }
        component.ShieldEntity = armshield;
        return true;
    }

    public bool SpawnArmace(EntityUid uid, ChangelingComponent component)
    {
        var mace = Spawn("ADTArmace", Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(mace);

        if (!_handsSystem.TryPickupAnyHand(uid, mace))
        {
            QueueDel(mace);
            return false;
        }
        component.ArmaceEntity = mace;
        return true;
    }

    private void RemoveBladeEntity(EntityUid uid, ChangelingComponent component)
    {
        if (!component.BladeEntity.HasValue)
            return;
        QueueDel(component.BladeEntity);
        _audioSystem.PlayPvs(component.SoundFlesh, uid);
        component.ArmBladeActive = false;

        var othersMessage = Loc.GetString("changeling-armblade-retract-others", ("user", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

        var selfMessage = Loc.GetString("changeling-armblade-retract-self");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
    }

    private void RemoveShieldEntity(EntityUid uid, ChangelingComponent component)
    {
        if (!component.ShieldEntity.HasValue)
            return;
        QueueDel(component.ShieldEntity);
        _audioSystem.PlayPvs(component.SoundFlesh, uid);
        component.ArmShieldActive = false;

        var othersMessage = Loc.GetString("changeling-armshield-retract-others", ("user", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

        var selfMessage = Loc.GetString("changeling-armshield-retract-self");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
    }

    private void RemoveArmaceEntity(EntityUid uid, ChangelingComponent component)
    {
        if (!component.ArmaceEntity.HasValue)
            return;
        QueueDel(component.ArmaceEntity);
        _audioSystem.PlayPvs(component.SoundFlesh, uid);
        component.ArmaceActive = false;

        var othersMessage = Loc.GetString("changeling-armace-retract-others", ("user", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

        var selfMessage = Loc.GetString("changeling-armace-retract-self");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
    }

    public void SpawnLingArmor(EntityUid uid, InventoryComponent inventory)
    {
        var helmet = Spawn("ClothingHeadHelmetLing", Transform(uid).Coordinates);
        var armor = Spawn("ClothingOuterArmorChangeling", Transform(uid).Coordinates);
        EnsureComp<UnremoveableComponent>(helmet);
        EnsureComp<UnremoveableComponent>(armor);

        _inventorySystem.TryUnequip(uid, "head", true, true, false, inventory);
        _inventorySystem.TryUnequip(uid, "outerClothing", true, true, false, inventory);

        _inventorySystem.TryEquip(uid, helmet, "head", true, true, false, inventory);
        _inventorySystem.TryEquip(uid, armor, "outerClothing", true, true, false, inventory);
    }
    #endregion
}
