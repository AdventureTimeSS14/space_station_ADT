// Simple Station

using System.Linq;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.ADT.Silicon;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Silicon.Charge;

public sealed class SiliconChargerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    //[Dependency] private readonly SharedSiliconChargerSystem _sharedCharger = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SiliconChargeSystem _silicon = default!;

    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<SiliconChargerComponent, RefreshPartsEvent>(OnRefreshParts);
        // SubscribeLocalEvent<SiliconChargerComponent, UpgradeExamineEvent>(OnExamineParts);

        SubscribeLocalEvent<SiliconChargerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SiliconChargerComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<SiliconChargerComponent, ComponentShutdown>(OnChargerShutdown);

        SubscribeLocalEvent<SiliconChargerComponent, StorageAfterOpenEvent>(HandleStateOpen);
        SubscribeLocalEvent<SiliconChargerComponent, StorageAfterCloseEvent>(HandleStateClose);
    }

    // TODO: Potentially refactor this so it chaches all found entities upon the storage being closed, or stepped on, etc.
    // Perhaps a variable for it? Open chargers like the pad wouldn't update to things picked up, but it seems silly to redo it each frame for closed ones.
    private void HandleStateOpen(EntityUid uid, SiliconChargerComponent component, ref StorageAfterOpenEvent _)
    {
        UpdateState(uid, component);
    }

    /// <inheritdoc cref="HandleStateOpen"/>
    private void HandleStateClose(EntityUid uid, SiliconChargerComponent component, ref StorageAfterCloseEvent _)
    {
        UpdateState(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        #region Entity Storage Chargers
        // Check for any chargers with the EntityStorageComponent.
        var entityStorageQuery = EntityQueryEnumerator<SiliconChargerComponent, EntityStorageComponent>();
        while (entityStorageQuery.MoveNext(out var uid, out var chargerComp, out var entStorage))
        {
            var wasActive = chargerComp.Active;
            chargerComp.Active = false;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var powerComp) && !powerComp.Powered)
            {
                if (chargerComp.Active != wasActive)
                    UpdateState(uid, chargerComp);

                continue;
            }

            foreach (var entity in entStorage.Contents.ContainedEntities)
            {
                chargerComp.Active = true;

                var chargeRate = chargerComp.ChargeMulti * frameTime * 10;

                HandleChargingEntity(entity, chargeRate, chargerComp, uid, frameTime);

                // Heat up the air in the charger.
                if (entStorage.Airtight)
                {
                    var curTemp = entStorage.Air.Temperature;

                    entStorage.Air.Temperature += curTemp < chargerComp.TargetTemp ? frameTime * chargerComp.ChargeMulti / 100 : 0;
                }
            }

            if (chargerComp.Active != wasActive)
                UpdateState(uid, chargerComp);
        }
        #endregion Entity Storage Chargers

        #region Step Trigger Chargers
        // Check for any chargers with the StepTriggerComponent.
        var stepQuery = EntityQueryEnumerator<SiliconChargerComponent, StepTriggerComponent>();
        while (stepQuery.MoveNext(out var uid, out var chargerComp, out _))
        {
            if (chargerComp.PresentEntities.Count == 0 ||
                TryComp<ApcPowerReceiverComponent>(uid, out var powerComp) && !powerComp.Powered)
            {
                if (chargerComp.Active)
                {
                    chargerComp.Active = false;
                    UpdateState(uid, chargerComp);
                }
                continue;
            }

            if (!chargerComp.Active)
            {
                chargerComp.Active = true;
                UpdateState(uid, chargerComp);
            }

            var chargeRate = frameTime * chargerComp.ChargeMulti / chargerComp.PresentEntities.Count;

            foreach (var entity in chargerComp.PresentEntities.ToList())
            {
                HandleChargingEntity(entity, chargeRate, chargerComp, uid, frameTime);
            }
        }
        #endregion Step Trigger Chargers
    }

    // Cleanup the sound stream when the charger is destroyed.
    private void OnChargerShutdown(EntityUid uid, SiliconChargerComponent component, ComponentShutdown args)
    {
        //component.SoundStream?.Stop(); /// modern.df ipc-locale
    }

    /// <summary>
    ///     Handles working out what entities need to have their batteries charged, or be burnt.
    /// </summary>
    private void HandleChargingEntity(EntityUid entity, float chargeRate, SiliconChargerComponent chargerComp, EntityUid chargerUid, float frameTime, bool burn = true)
    {
        var entitiesToCharge = SearchThroughEntities(entity, burn);

        if (entitiesToCharge.Count == 0)
            return;

        chargeRate *= chargerComp.PartsChargeMulti;

        var entitiesToChargeCount = entitiesToCharge.Count;

        foreach (var (entityToCharge, batteryComp) in entitiesToCharge.ToList())
        {
            if (batteryComp != null && batteryComp.CurrentCharge >= batteryComp.MaxCharge)
                entitiesToChargeCount--; // Remove any full batteries from the count, so they don't impact charge rate.
        }

        // Now we charge the entities we found.
        chargeRate /= entitiesToChargeCount;

        foreach (var (entityToCharge, batteryComp) in entitiesToCharge.ToList())
        {
            if (batteryComp != null)
                ChargeBattery(entityToCharge, batteryComp, chargeRate, chargerComp, chargerUid);
            else if (TryComp<DamageableComponent>(entityToCharge, out var damageComp))
                BurnEntity(entityToCharge, damageComp, frameTime, chargerComp, chargerUid);
        }
    }

    private List<(EntityUid, BatteryComponent?)> SearchThroughEntities(EntityUid entity, bool burn = true)
    {
        var entitiesToCharge = new List<(EntityUid, BatteryComponent?)>();

        // If the given entity is a silicon, charge their respective battery.
        if (_silicon.TryGetSiliconBattery(entity, out var siliconBatteryComp, out var siliconBatteryUid))
        {
            entitiesToCharge.Add((siliconBatteryUid, siliconBatteryComp));
        }

        // Or if the given entity has a battery, charge it.
        else if (!HasComp<UnremoveableComponent>(entity) && // Should probably be charged by the entity holding it. Might be too small to be safe.
            TryComp<BatteryComponent>(entity, out var batteryComp))
        {
            entitiesToCharge.Add((entity, batteryComp));
        }

        // Or if the given entity contains a battery, charge it.
        else if (!HasComp<UnremoveableComponent>(entity) && // Should probably be charged by the entity holding it. Might be too small to be safe.
                TryComp<PowerCellSlotComponent>(entity, out var cellSlotComp) &&
                _itemSlots.TryGetSlot(entity, cellSlotComp.CellSlotId, out var slot) &&
                TryComp<BatteryComponent>(slot.Item, out var cellBattComp))
        {
            entitiesToCharge.Add((slot.Item.Value, cellBattComp));
        }

        // Or if the given entity is fleshy, burn the fucker.
        else if (burn &&
                TryComp<DamageableComponent>(entity, out var damageComp) &&
                damageComp.DamageContainerID == "Biological")
        {
            entitiesToCharge.Add((entity, null));
        }

        // Now the weird part, we check for any inventories the entities contained may have, and run this function on any entities contained, for a recursive charging effect.
        if (TryComp<HandsComponent>(entity, out var handsComp))
        {
            foreach (var heldEntity in _hands.EnumerateHeld(entity, handsComp))
            {
                entitiesToCharge.AddRange(SearchThroughEntities(heldEntity));
            }
        }
        if (TryComp<InventoryComponent>(entity, out var inventoryComp))
        {
            if (_inventory.TryGetSlots(entity, out var slots))
            {
                foreach (var slot in slots)
                {
                    if (_inventory.TryGetSlotEntity(entity, slot.Name, out var slotItem))
                        entitiesToCharge.AddRange(SearchThroughEntities(slotItem.Value));
                }
            }
        }
        /// modern.df ipc-locale
        /*
        if (TryComp<ServerStorageComponent>(entity, out var storageComp))
        {
            foreach (var containedEntity in storageComp.StoredEntities!)
            {
                entitiesToCharge.AddRange(SearchThroughEntities(containedEntity));
            }
        }
        */
        if (TryComp<EntityStorageComponent>(entity, out var entStorage))
        {
            foreach (var containedEntity in entStorage.Contents.ContainedEntities)
            {
                entitiesToCharge.AddRange(SearchThroughEntities(containedEntity));
            }
        }

        return entitiesToCharge;
    }

    private void ChargeBattery(EntityUid entity, BatteryComponent batteryComp, float chargeRate, SiliconChargerComponent chargerComp, EntityUid chargerUid)
    {
        // Do some math so a charger never charges a battery from zero to full in less than the minimum time, just for the effect of it.
        if (chargerComp.ChargeMulti * 10 > batteryComp.MaxCharge / chargerComp.MinChargeTime)
            chargeRate /= chargerComp.ChargeMulti * 10 / (batteryComp.MaxCharge / chargerComp.MinChargeTime);

        if (batteryComp.CurrentCharge + chargeRate < batteryComp.MaxCharge)
            _battery.SetCharge(entity, batteryComp.CurrentCharge + chargeRate, batteryComp);
        else
            _battery.SetCharge(entity, batteryComp.MaxCharge, batteryComp);

        // If the battery is too small, explode it.
        if ((batteryComp.MaxCharge - batteryComp.CurrentCharge) * 1.2 + batteryComp.MaxCharge < chargerComp.MinChargeSize)
        {
            if (TryComp<ExplosiveComponent>(entity, out var explosiveComp))
                _explosion.TriggerExplosive(entity, explosiveComp);
            else
                _explosion.QueueExplosion(entity, "Default", batteryComp.MaxCharge / 50, 1.5f, 200, user: chargerUid);
        }
    }

    private void BurnEntity(EntityUid entity, DamageableComponent damageComp, float frameTime, SiliconChargerComponent chargerComp, EntityUid chargerUid)
    {
        var damage = new DamageSpecifier(_prototypes.Index<DamageTypePrototype>(chargerComp.DamageType), frameTime * chargerComp.ChargeMulti / 100);
        var damageDealt = _damageable.TryChangeDamage(entity, damage, false, true, damageComp, chargerUid);

        if (damageDealt != null && chargerComp.WarningTime < _timing.CurTime)
        {
            var popupBurn = Loc.GetString(chargerComp.OverheatString);
            _popup.PopupEntity(popupBurn, entity, entity, PopupType.MediumCaution);

            chargerComp.WarningTime = TimeSpan.FromSeconds(_random.Next(3, 7)) + _timing.CurTime;
        }
    }

    // private void OnRefreshParts(EntityUid uid, SiliconChargerComponent component, RefreshPartsEvent args)
    // {
    //     var chargeMod = args.PartRatings[component.ChargeSpeedPart];
    //     var efficiencyMod = args.PartRatings[component.ChargeEfficiencyPart];

    //     component.PartsChargeMulti = chargeMod * component.UpgradePartsMulti;
    //     // TODO: Variable power draw, with efficiency.
    // }

    // private void OnExamineParts(EntityUid uid, SiliconChargerComponent component, UpgradeExamineEvent args)
    // {
    //     args.AddPercentageUpgrade("silicon-charger-chargerate-string", component.PartsChargeMulti);
    //     // TODO: Variable power draw, with efficiency.
    // }

    #region Charger specific
    #region Step Trigger Chargers
    // When an entity starts colliding with the charger, add it to the list of entities present on the charger if it has the StepTriggerComponent.
    private void OnStartCollide(EntityUid uid, SiliconChargerComponent component, ref StartCollideEvent args)
    {
        if (!HasComp<StepTriggerComponent>(uid))
            return;

        var target = args.OtherEntity;

        if (component.PresentEntities.Contains(target))
            return;

        if (component.PresentEntities.Count >= component.MaxEntities)
        {
            _popup.PopupEntity(Loc.GetString("silicon-charger-list-full", ("charger", args.OurEntity)), target, target);
            return;
        }

        component.PresentEntities.Add(target);
    }

    // When an entity stops colliding with the charger, remove it from the list of entities present on the charger.
    private void OnEndCollide(EntityUid uid, SiliconChargerComponent component, ref EndCollideEvent args)
    {
        if (!HasComp<StepTriggerComponent>(uid))
            return;

        var target = args.OtherEntity;

        if (component.PresentEntities.Contains(target))
        {
            component.PresentEntities.Remove(target);
        }
    }
    #endregion Step Trigger Chargers
    #endregion Charger specific

    public void UpdateState(EntityUid uid, SiliconChargerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Active)
        {
            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, SiliconChargerVisualState.Charging);

            // If we're in prediction, return since Client doesn't have the information needed to handle this.
            // Didn't seem to matter in practice, but probably for the best.
            if (_timing.InPrediction)
                return;

            //if (component.SoundLoop != null && component.SoundStream == null)
            //    component.SoundStream =
            //        _audio.PlayPvs(component.SoundLoop, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));
        }
        else
        {
            var state = SiliconChargerVisualState.Normal;

            if (EntityManager.TryGetComponent<EntityStorageComponent>(uid, out var storageComp) && storageComp.Open)
                state = SiliconChargerVisualState.NormalOpen;

            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, state);

            // If we're in prediction, return since Client doesn't have the information needed to handle this.
            // Didn't seem to matter in practice, but probably for the best.
            if (_timing.InPrediction)
                return;

            //component.SoundStream?.Stop();
            //component.SoundStream = null;
        }
    }
}
