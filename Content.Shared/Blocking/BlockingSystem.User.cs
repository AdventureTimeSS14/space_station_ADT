using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
//ADT-Tweak-Start
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
//ADT-Tweak-End


namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!; //ADT-Tweak
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!; //ADT-Tweak

    private void InitializeUser()
    {
        SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);
        SubscribeLocalEvent<BlockingComponent, DamageModifyEvent>(OnDamageModified);

        SubscribeLocalEvent<BlockingUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<BlockingUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BlockingUserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<BlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);

        SubscribeLocalEvent<BlockingComponent, ItemToggleActivateAttemptEvent>(OnItemToggleAttempt); //ADT-Tweak
        SubscribeLocalEvent<BlockingComponent, ChargeChangedEvent>(OnChargeChanged); //ADT-Tweak
    }

    private void OnParentChanged(EntityUid uid, BlockingUserComponent component, ref EntParentChangedMessage args)
    {
        UserStopBlocking(uid, component);
    }

    private void OnInsertAttempt(EntityUid uid, BlockingUserComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        UserStopBlocking(uid, component);
    }

    private void OnAnchorChanged(EntityUid uid, BlockingUserComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        UserStopBlocking(uid, component);
    }

    private void OnUserDamageModified(EntityUid uid, BlockingUserComponent component, DamageModifyEvent args)
    {
        if (component.BlockingItem is not { } item || !TryComp<BlockingComponent>(item, out var blocking))
            return;

        if (args.Damage.GetTotal() <= 0)
            return;

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
        blockFraction = Math.Clamp(blockFraction, 0, 1);

        //ADT-Tweak-Start
        blockFraction = ApplyBatteryLimitToBlockFraction(item, blocking, blockFraction, args.OriginalDamage);

        if (blockFraction <= 0)
            return;
        //ADT-Tweak-End

        _damageable.TryChangeDamage((item, dmgComp), blockFraction * args.OriginalDamage);

        var modify = new DamageModifierSet();
        foreach (var key in dmgComp.Damage.DamageDict.Keys)
        {
            modify.Coefficients.TryAdd(key, 1 - blockFraction);
        }

        //ADT-Tweak-Start
        if (HasEnoughBatteryCharge(item, blocking))
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

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
        ConsumeBatteryCharge(uid, component, (float)args.Damage.GetTotal()); //ADT-Tweak
    }

    private void OnEntityTerminating(EntityUid uid, BlockingUserComponent component, ref EntityTerminatingEvent args)
    {
        if (!TryComp<BlockingComponent>(component.BlockingItem, out var blockingComponent))
            return;

        StopBlockingHelper(component.BlockingItem.Value, blockingComponent, uid);

    }

    //ADT-Tweak-Start
    private void OnItemToggleAttempt(Entity<BlockingComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (!entity.Comp.IsCharging)
            return;

        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        if (battery.CurrentCharge <= 0)
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("handheld-light-component-cell-dead-message");
        }
    }

    private void OnChargeChanged(Entity<BlockingComponent> entity, ref ChargeChangedEvent args)
    {
        if (!entity.Comp.IsCharging)
            return;

        if (TryComp<BatteryComponent>(entity, out var battery) && battery.CurrentCharge <= 0)
        {
            if (TryComp<ItemToggleComponent>(entity, out var itemToggle))
                _itemToggleSystem.TryDeactivate((entity, itemToggle), null);

            _popupSystem.PopupPredicted(Loc.GetString("inducer-empty"), entity, entity);

            if (entity.Comp.User != null)
                StopBlockingHelper(entity, entity.Comp, entity.Comp.User.Value);
        }
    }

    private float ApplyBatteryLimitToBlockFraction(EntityUid uid, BlockingComponent component, float blockFraction, DamageSpecifier originalDamage)
    {
        if (!component.IsCharging)
            return blockFraction;

        if (!TryComp<BatteryComponent>(uid, out var battery))
            return blockFraction;

        var originalTotalDamage = (float)originalDamage.GetTotal();
        var desiredShieldDamage = blockFraction * originalTotalDamage;
        var maxBlockableDamage = (float)(battery.CurrentCharge / component.EnergyCostPerHit);

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

        return battery.CurrentCharge <= 0;
    }

    private void ConsumeBatteryCharge(EntityUid uid, BlockingComponent component, float damage)
    {
        if (component.IsCharging && TryComp<BatteryComponent>(uid, out var battery))
        {
            var chargeToConsume = component.EnergyCostPerHit * damage;
            var newCharge = Math.Max(0, battery.CurrentCharge - chargeToConsume);

            _batterySystem.SetCharge(uid, newCharge);
        }
    }
    //ADT-Tweak-End

    /// <summary>
    /// Check for the shield and has the user stop blocking
    /// Used where you'd like the user to stop blocking, but also don't want to remove the <see cref="BlockingUserComponent"/>
    /// </summary>
    /// <param name="uid">The user blocking</param>
    /// <param name="component">The <see cref="BlockingUserComponent"/></param>
    private void UserStopBlocking(EntityUid uid, BlockingUserComponent component)
    {
        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockComp) && blockComp.IsBlocking)
            StopBlocking(component.BlockingItem.Value, blockComp, uid);
    }
}
