using Content.Shared.Bed.Sleep;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Ghost.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.ADT.IPC;

public abstract class SharedSiliconSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] protected MovementSpeedModifierSystem MoveMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<SiliconComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<SiliconComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<SiliconComponent, TryingToSleepEvent>(OnTryingToSleep);
        SubscribeLocalEvent<SiliconComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SiliconComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<SiliconComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
    }

    private bool IsBatterySlot(EntityUid uid, ItemSlot slot)
    {
        return TryComp<PowerCellSlotComponent>(uid, out var cellSlot)
            && _itemSlots.TryGetSlot(uid, cellSlot.CellSlotId, out var batterySlot)
            && batterySlot == slot;
    }

    private void OnItemSlotInsertAttempt(Entity<SiliconComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled || args.User != ent.Owner || !IsBatterySlot(ent, args.Slot))
            return;

        args.Cancelled = true;
    }

    private void OnItemSlotEjectAttempt(Entity<SiliconComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled || args.User != ent.Owner || !IsBatterySlot(ent, args.Slot))
            return;

        args.Cancelled = true;
    }

    private void OnRefreshMovespeed(Entity<SiliconComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.BatteryPowered)
            return;

        int? closest = null;
        foreach (var threshold in ent.Comp.SpeedModifierThresholds.Keys)
        {
            if (ent.Comp.ChargeState >= threshold && (closest == null || threshold > closest))
                closest = threshold;
        }

        if (closest == null)
            return;

        var speedMod = ent.Comp.SpeedModifierThresholds[closest.Value];
        args.ModifySpeed(speedMod, speedMod);
    }

    private void OnTryingToSleep(Entity<SiliconComponent> ent, ref TryingToSleepEvent args)
    {
        if (ent.Comp.CanSleep || ent.Comp.Drained)
            return;

        args.Cancelled = true;
    }

    protected virtual void OnMobStateChanged(Entity<SiliconComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.DisableLightOnDeath)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            if (!_pointLight.TryGetLight(ent.Owner, out var light) || !light.Enabled)
                return;

            _pointLight.SetEnabled(ent.Owner, false, light);
            ent.Comp.LightDisabledByDeath = true;
            return;
        }

        if (args.OldMobState != MobState.Dead || !ent.Comp.LightDisabledByDeath)
            return;

        _pointLight.SetEnabled(ent.Owner, true);
        ent.Comp.LightDisabledByDeath = false;
    }

    private void OnLockToggled(Entity<SiliconComponent> ent, ref LockToggledEvent args)
    {
        if (!ent.Comp.LockBatterySlot
            || !TryComp<PowerCellSlotComponent>(ent, out var cellSlot)
            || !TryComp<ItemSlotsComponent>(ent, out var itemSlots)
            || !_itemSlots.TryGetSlot((ent, itemSlots), cellSlot.CellSlotId, out var slot))
            return;

        _itemSlots.SetLock((ent, itemSlots), slot, args.Locked);
    }

    private void OnLockToggleAttempt(Entity<SiliconComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!ent.Comp.LockBatterySlot || args.Silent || args.User == ent.Owner || HasComp<GhostComponent>(args.User))
            return;

        _popup.PopupEntity(Loc.GetString("batteryslotrequireslock-component-alert-owner", ("user", Identity.Entity(args.User, EntityManager))), ent, ent, PopupType.Large);
    }
}
