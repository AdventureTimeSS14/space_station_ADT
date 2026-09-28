using Content.Shared.Blocking.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Examine;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
<<<<<<< ours
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!; //ADT-Tweak
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!; //ADT-Tweak
||||||| base
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
=======
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
>>>>>>> theirs

    private void InitializeUser()
    {
        SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);
        SubscribeLocalEvent<BlockingUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<BlockingUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BlockingUserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<BlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);

        SubscribeLocalEvent<BlockingComponent, ItemToggleActivateAttemptEvent>(OnItemToggleAttempt); //ADT-Tweak
        SubscribeLocalEvent<BlockingComponent, ChargeChangedEvent>(OnChargeChanged); //ADT-Tweak
        SubscribeLocalEvent<BlockingComponent, ExaminedEvent>(OnBatteryExamined); //ADT-Tweak
    }

    private void OnParentChanged(Entity<BlockingUserComponent> entity, ref EntParentChangedMessage args)
    {
        UserStopBlocking(entity);
    }

    private void OnInsertAttempt(Entity<BlockingUserComponent> entity, ref ContainerGettingInsertedAttemptEvent args)
    {
        UserStopBlocking(entity);
    }

    private void OnAnchorChanged(Entity<BlockingUserComponent> entity, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        UserStopBlocking(entity);
    }

    private void OnUserDamageModified(Entity<BlockingUserComponent> entity, ref DamageModifyEvent args)
    {
        if (entity.Comp.BlockingItem is not { } item || !_blockQuery.TryComp(item, out var blocking))
            return;

        if (args.Damage.GetTotal() <= 0)
            return;

<<<<<<< ours
        // A shield should only block damage it can itself absorb. To determine that we need the Damageable component on it.
        if (!TryComp<DamageableComponent>(item, out var dmgComp))
            return;

        //ADT-Tweak-Start
        if (blocking.IsToggle)
        {
            if (TryComp<ItemToggleComponent>(item, out var itemToggle) && !itemToggle.Activated)
                return;
        }
        //ADT-Tweak-End

        var blockFraction = blocking.IsBlocking ? blocking.ActiveBlockFraction : blocking.PassiveBlockFraction;
        var modifier = blocking.IsBlocking ? blocking.ActiveBlockDamageModifier : blocking.PassiveBlockDamageModifer;
||||||| base
        // A shield should only block damage it can itself absorb. To determine that we need the Damageable component on it.
        if (!TryComp<DamageableComponent>(item, out var dmgComp))
            return;

        var blockFraction = blocking.IsBlocking ? blocking.ActiveBlockFraction : blocking.PassiveBlockFraction;
        var modifier = blocking.IsBlocking ? blocking.ActiveBlockDamageModifier : blocking.PassiveBlockDamageModifer;
=======
        var blockFraction = blocking.IsRaised ? blocking.ActiveBlockFraction : blocking.PassiveBlockFraction;
>>>>>>> theirs
        blockFraction = Math.Clamp(blockFraction, 0, 1);
<<<<<<< ours

        //ADT-Tweak-Start
        blockFraction = ApplyBatteryLimitToBlockFraction(item, blocking, blockFraction, args.OriginalDamage);

        if (blockFraction <= 0)
            return;
        //ADT-Tweak-End

        _damageable.TryChangeDamage((item, dmgComp), blockFraction * args.OriginalDamage);
||||||| base
        _damageable.TryChangeDamage((item, dmgComp), blockFraction * args.OriginalDamage);
=======
>>>>>>> theirs

        // This is how much damage the shield is attempting to block
        var split = args.OriginalDamage * blockFraction;
        var damage = _damageable.ChangeDamage(item, split);

<<<<<<< ours
        //ADT-Tweak-Start
        if (blocking.IsCharging && HasEnoughBatteryCharge(item, blocking))
        {
            UserStopBlocking(uid, component);
            return;
        }
        //ADT-Tweak-End

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modify);

        if (blocking.IsBlocking && !args.Damage.Equals(args.OriginalDamage))
        {
            _audio.PlayPvs(blocking.BlockSound, uid);
        }
    }
||||||| base
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modify);

        if (blocking.IsBlocking && !args.Damage.Equals(args.OriginalDamage))
        {
            _audio.PlayPvs(blocking.BlockSound, uid);
        }
    }
=======
        // Of the damage that went through, reduce by the appropriate blocking modifiers.
        var modifier = GetBlockingModifier((item, blocking));
        var blowthrough = DamageSpecifier.ApplyModifierSet(split, modifier);
>>>>>>> theirs

<<<<<<< ours
    private void OnDamageModified(EntityUid uid, BlockingComponent component, DamageModifyEvent args)
    {
        //ADT-Tweak-Start
        if (component.IsToggle)
        {
            if (TryComp<ItemToggleComponent>(uid, out var itemToggle) && !itemToggle.Activated)
                return;
        }
        //ADT-Tweak-End

        var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;
        if (modifier == null)
        {
            return;
        }
||||||| base
    private void OnDamageModified(EntityUid uid, BlockingComponent component, DamageModifyEvent args)
    {
        var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;
        if (modifier == null)
        {
            return;
        }
=======
        args.Damage *= 1f - blockFraction;
        args.Damage += blowthrough;
>>>>>>> theirs

<<<<<<< ours
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
        ConsumeBatteryCharge(uid, component, (float)args.Damage.GetTotal()); //ADT-Tweak
||||||| base
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
=======
        if (blocking.IsRaised && damage.AnyPositive())
            _audio.PlayPvs(blocking.BlockSound, entity);
>>>>>>> theirs
    }

    private void OnEntityTerminating(Entity<BlockingUserComponent> entity, ref EntityTerminatingEvent args)
    {
        if (!_blockQuery.TryComp(entity.Comp.BlockingItem, out var blockComponent))
            return;

        StopBlocking((entity.Comp.BlockingItem.Value, blockComponent), entity);
    }

    //ADT-Tweak-Start
    private void OnItemToggleAttempt(Entity<BlockingComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (!entity.Comp.IsCharging)
            return;

        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        if (_batterySystem.GetCharge((entity, battery)) <= 0.1f)
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("handheld-light-component-cell-dead-message");
        }
    }

    private void OnChargeChanged(Entity<BlockingComponent> entity, ref ChargeChangedEvent args)
    {
        if (!entity.Comp.IsCharging)
            return;

        if (!TryComp<BatteryComponent>(entity, out var battery) || _batterySystem.GetCharge((entity, battery)) > 0.1f)
            return;

        if (TryComp<ItemToggleComponent>(entity, out var itemToggle))
            _itemToggleSystem.TryDeactivate((entity, itemToggle), null);

        _popupSystem.PopupPredicted(Loc.GetString("inducer-empty"), entity, entity);

        if (entity.Comp.User != null)
            StopBlockingHelper(entity, entity.Comp, entity.Comp.User.Value);

    }

    private float ApplyBatteryLimitToBlockFraction(EntityUid uid, BlockingComponent component, float blockFraction, DamageSpecifier originalDamage)
    {
        if (!component.IsCharging)
            return blockFraction;

        if (!TryComp<BatteryComponent>(uid, out var battery))
            return blockFraction;

        var originalTotalDamage = (float)originalDamage.GetTotal();
        var desiredShieldDamage = blockFraction * originalTotalDamage;
        var maxBlockableDamage = (float)(_batterySystem.GetCharge((uid, battery)) / component.EnergyCostPerHit);

        if (desiredShieldDamage <= maxBlockableDamage)
            return blockFraction;

        var limitedBlockFraction = maxBlockableDamage / originalTotalDamage;
        limitedBlockFraction = Math.Clamp(limitedBlockFraction, 0, blockFraction);

        return limitedBlockFraction;
    }

    private bool HasEnoughBatteryCharge(EntityUid uid, BlockingComponent component)
    {
        if (!component.IsCharging)
            return true;

        if (!TryComp<BatteryComponent>(uid, out var battery))
            return true;

        return _batterySystem.GetCharge((uid, battery)) <= 0.1f;
    }

    private void ConsumeBatteryCharge(EntityUid uid, BlockingComponent component, float damage)
    {
        if (component.IsCharging && TryComp<BatteryComponent>(uid, out var battery))
        {
            var chargeToConsume = component.EnergyCostPerHit * damage;
            var newCharge = Math.Max(0, _batterySystem.GetCharge((uid, battery)) - chargeToConsume);

            _batterySystem.SetCharge(uid, newCharge);
        }
    }

    private void OnBatteryExamined(Entity<BlockingComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        if (!ent.Comp.IsCharging)
            return;

        var chargePercent = _batterySystem.GetChargeLevel((ent.Owner, battery)) * 100;
        args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{chargePercent:F0}")));
    }
    //ADT-Tweak-End

    /// <summary>
    /// Check for the shield and has the user stop blocking
    /// Used where you'd like the user to stop blocking, but also don't want to remove the <see cref="BlockingUserComponent"/>
    /// </summary>
    /// <param name="entity">The user blocking</param>
    private void UserStopBlocking(Entity<BlockingUserComponent> entity)
    {
        if (!_blockQuery.TryComp(entity.Comp.BlockingItem, out var blockComponent))
            return;

        LowerShield((entity.Comp.BlockingItem.Value, blockComponent), entity);
    }
}
