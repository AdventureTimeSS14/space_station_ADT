using System.Linq;
using System.Numerics;
using Content.Server.Advertise;
using Content.Server.Advertise.EntitySystems;
using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.Store.Components;
using Content.Server.ADT.Economy;
using Content.Shared.ADT.Economy;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Advertise.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SpeakOnUIClosedSystem _speakOnUIClosed = default!;
        //ADT-Economy-Start
        [Dependency] private readonly BankCardSystem _bankCard = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        //ADT-Economy-End
        [Dependency] private readonly SharedPointLightSystem _light = default!;
        [Dependency] private readonly EmagSystem _emag = default!;

        private const float WallVendEjectDistanceFromWall = 1f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage); //ADT-Economy
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineComponent, EmpPulseEvent>(OnEmpPulse);

            SubscribeLocalEvent<VendingMachineComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);

            Subs.BuiEvents<VendingMachineComponent>(VendingMachineUiKey.Key, subs =>
            {
                subs.Event<VendingMachineEjectMessage>(OnInventoryEjectMessage);
                subs.Event<VendingMachineEjectCountMessage>(OnInventoryEjectCountMessage);  // ADT vending eject count
            });

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            SubscribeLocalEvent<VendingMachineComponent, RestockDoAfterEvent>(OnDoAfter);

            //ADT-Economy-Start
            SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineWithdrawMessage>(OnWithdrawMessage);
            //ADT-Economy-End

            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);
        }

        private void OnVendingPrice(EntityUid uid, VendingMachineComponent component, ref PriceCalculationEvent args)
        {
            var price = 0.0;

            foreach (var entry in component.Inventory.Values)
            {
                if (!PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto))
                {
                    Log.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(uid)} vending.");
                    continue;
                }

                price += entry.Amount * _pricing.GetEstimatedPrice(proto);
            }

            args.Price += price;
        }

        protected override void OnMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
        {
            base.OnMapInit(uid, component, args);

            if (HasComp<ApcPowerReceiverComponent>(uid))
            {
                TryUpdateVisualState(uid, component);
            }
        }

        private void OnActivatableUIOpenAttempt(EntityUid uid, VendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }

        private void UpdateVendingMachineInterfaceState(EntityUid uid, VendingMachineComponent component)
        {
            var state = new VendingMachineInterfaceState(GetAllInventory(uid, component), component.PriceMultiplier,
                component.Credits); //ADT-Economy

            _userInterfaceSystem.SetUiState(uid, VendingMachineUiKey.Key, state);
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Type, args.ID, component, 1);  // ADT vending eject count
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, component);
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
        {
            vendComponent.Broken = true;
            TryUpdateVisualState(uid, vendComponent);
        }

        private void OnDamage(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args) //ADT-Economy
        {
            if (component.Broken || component.DispenseOnHitCoolingDown ||
                component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageIncreased && args.DamageDelta.GetTotal() >= component.DispenseOnHitThreshold &&
                _random.Prob(component.DispenseOnHitChance.Value))
            {
                if (component.DispenseOnHitCooldown > 0f)
                    component.DispenseOnHitCoolingDown = true;
                EjectRandom(uid, throwItem: true, forceEject: true, component);
            }
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, throwItem: true, forceEject: false, component);
        }

        private void OnDoAfter(EntityUid uid, VendingMachineComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Used == null)
                return;

            if (!TryComp<VendingMachineRestockComponent>(args.Args.Used, out var restockComponent))
            {
                Log.Error($"{ToPrettyString(args.Args.User)} tried to restock {ToPrettyString(uid)} with {ToPrettyString(args.Args.Used.Value)} which did not have a VendingMachineRestockComponent.");
                return;
            }

            TryRestockInventory(uid, component);

            Popup.PopupEntity(Loc.GetString("vending-machine-restock-done-self", ("target", uid)), args.Args.User, args.Args.User, PopupType.Medium);
            var othersFilter = Filter.PvsExcept(args.Args.User);
            Popup.PopupEntity(Loc.GetString("vending-machine-restock-done-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", uid)), args.Args.User, othersFilter, true, PopupType.Medium);

            Audio.PlayPvs(restockComponent.SoundRestockDone, uid, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

            Del(args.Args.Used.Value);

            args.Handled = true;
        }

        //ADT-Economy-Start
        private void OnInteractUsing(EntityUid uid, VendingMachineComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (component.Broken || !this.IsPowered(uid, EntityManager))
                return;

            if (!TryComp<CurrencyComponent>(args.Used, out var currency) ||
                !currency.Price.Keys.Contains(component.CurrencyType))
                return;

            var stack = Comp<StackComponent>(args.Used);
            component.Credits += stack.Count;
            Del(args.Used);
            UpdateVendingMachineInterfaceState(uid, component);
            Audio.PlayPvs(component.SoundInsertCurrency, uid);
            args.Handled = true;
        }

        protected override int GetEntryPrice(EntityPrototype proto)
        {
            var price = (int)_pricing.GetEstimatedPrice(proto);
            return price > 0 ? price : 25;
        }

        private int GetPrice(VendingMachineInventoryEntry entry, VendingMachineComponent comp, int count)
        {
            return (int)(entry.Price * count * comp.PriceMultiplier);
        }

        private void OnWithdrawMessage(EntityUid uid, VendingMachineComponent component, VendingMachineWithdrawMessage args)
        {
            _stackSystem.Spawn(component.Credits, PrototypeManager.Index(component.CreditStackPrototype),
                Transform(uid).Coordinates);
            component.Credits = 0;
            Audio.PlayPvs(component.SoundWithdrawCurrency, uid);

            UpdateVendingMachineInterfaceState(uid, component);
        }

        private void OnInventoryEjectCountMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectCountMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Entry.Type, args.Entry.ID, component, args.Count);
        }

        //ADT-Economy-End

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.CanShoot"/> property of the vending machine.
        /// </summary>
        public void SetShooting(EntityUid uid, bool canShoot, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanShoot = canShoot;
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.Contraband"/> property of the vending machine.
        /// </summary>
        public void SetContraband(EntityUid uid, bool contraband, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Contraband = contraband;
            Dirty(uid, component);
        }

        public void Deny(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Denying)
                return;

            vendComponent.Denying = true;
            Audio.PlayPvs(vendComponent.SoundDeny, uid, AudioParams.Default.WithVolume(-2f));
            TryUpdateVisualState(uid, vendComponent);
        }

        /// <summary>
        /// Checks if the user is authorized to use this vending machine
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity trying to use the vending machine</param>
        /// <param name="vendComponent"></param>
        public bool IsAuthorized(EntityUid uid, EntityUid sender, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
                return true;

            if (_accessReader.IsAllowed(sender, uid, accessReader))
                return true;

            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender); //ADT-Economy
            Deny(uid, vendComponent);
            return false;
        }

        /// <summary>
        /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
        /// or the item doesn't exist in its inventory.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
        /// <param name="vendComponent"></param>
        public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, int count, VendingMachineComponent? vendComponent = null, EntityUid? sender = null) // ADT vending eject count
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Ejecting || vendComponent.Broken || !this.IsPowered(uid, EntityManager))
            {
                return;
            }

            var entry = GetEntry(uid, itemId, type, vendComponent);

            if (entry == null)
            {
                //ADT-Economy-Start
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, sender.Value);
                //ADT-Economy-End

                Deny(uid, vendComponent);
                return;
            }

            if (entry.Amount <= 0)
            {
                //ADT-Economy-Start
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, sender.Value);
                //ADT-Economy-End

                Deny(uid, vendComponent);
                return;
            }

            if (string.IsNullOrEmpty(entry.ID))
                return;

            //ADT-Economy-Start
            var price = GetPrice(entry, vendComponent, count);
            if (price > 0 && !vendComponent.AllForFree && sender.HasValue && !_tag.HasTag(sender.Value, "IgnoreBalanceChecks"))
            {
                var success = false;
                if (vendComponent.Credits >= price)
                {
                    vendComponent.Credits -= price;
                    success = true;
                }
                else
                {
                    var items = _accessReader.FindPotentialAccessItems(sender.Value);
                    foreach (var item in items)
                    {
                        var nextItem = item;
                        if (TryComp(item, out PdaComponent? pda) && pda.ContainedId is { Valid: true } id)
                            nextItem = id;

                        if (!TryComp<BankCardComponent>(nextItem, out var bankCard) || !bankCard.AccountId.HasValue
                            || !_bankCard.TryGetAccount(bankCard.AccountId.Value, out var account)
                            || account.Balance < price)
                            continue;

                        _bankCard.TryChangeBalance(bankCard.AccountId.Value, -price);
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-no-balance"), uid);
                    Deny(uid, vendComponent);
                    return;
                }
            }
            vendComponent.NextItemCount = count;
            //ADT-Economy-End

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            vendComponent.NextItemToEject = entry.ID;
            vendComponent.ThrowNextItem = throwItem;

            if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
                _speakOnUIClosed.TrySetFlag((uid, speakComponent));

            entry.Amount -= (uint)count;    // ADT vending eject count
            Dirty(uid, vendComponent);
            TryUpdateVisualState(uid, vendComponent);
            Audio.PlayPvs(vendComponent.SoundVend, uid);
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="component"></param>
        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component, int count)    // ADT vending eject count
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, type, itemId, component.CanShoot, count, component, sender); // ADT vending eject count
            }
        }

        /// <summary>
        /// Tries to update the visuals of the component based on its current state.
        /// </summary>
        public void TryUpdateVisualState(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var finalState = VendingMachineVisualState.Normal;
            if (vendComponent.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (vendComponent.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (vendComponent.Denying)
            {
                finalState = VendingMachineVisualState.Deny;
            }
            else if (!this.IsPowered(uid, EntityManager))
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (_light.TryGetLight(uid, out var pointlight))
            {
                var lightState = finalState != VendingMachineVisualState.Broken && finalState != VendingMachineVisualState.Off;
                _light.SetEnabled(uid, lightState, pointlight);
            }

            _appearanceSystem.SetData(uid, VendingMachineVisuals.VisualState, finalState);
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        /// <param name="vendComponent"></param>
        public void EjectRandom(EntityUid uid, bool throwItem, bool forceEject = false, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var availableItems = GetAvailableInventory(uid, vendComponent);
            if (availableItems.Count <= 0)
                return;

            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                vendComponent.NextItemToEject = item.ID;
                vendComponent.ThrowNextItem = throwItem;
                var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
                if (entry != null)
                    entry.Amount--;
                EjectItem(uid, 1, vendComponent, forceEject);   // ADT vending eject count
            }
            else
            {
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, 1, vendComponent);   // ADT vending eject count
            }
        }

        private void EjectItem(EntityUid uid, int count, VendingMachineComponent? vendComponent = null, bool forceEject = false)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                TryUpdateVisualState(uid, vendComponent);

            if (string.IsNullOrEmpty(vendComponent.NextItemToEject))
            {
                vendComponent.ThrowNextItem = false;
                return;
            }

            // Default spawn coordinates
            var spawnCoordinates = Transform(uid).Coordinates;

            //Make sure the wallvends spawn outside of the wall.

            if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
            {

                var offset = wallMountComponent.Direction.ToWorldVec() * WallVendEjectDistanceFromWall;
                spawnCoordinates = spawnCoordinates.Offset(offset);
            }
            // ADT vending eject count start
            for (var i = 0; i < count; i++)
            {
                var ent = Spawn(vendComponent.NextItemToEject, spawnCoordinates);

                if (vendComponent.ThrowNextItem)
                {
                    var range = vendComponent.NonLimitedEjectRange;
                    var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
                }
            }
            // ADT vending eject count end

            vendComponent.NextItemToEject = null;
            vendComponent.ThrowNextItem = false;
            vendComponent.NextItemCount = 1;    // ADT vending eject count
        }

        private VendingMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, InventoryType type, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return null;

            if (type == InventoryType.Emagged && _emag.CheckFlag(uid, EmagType.Interaction))
                return component.EmaggedInventory.GetValueOrDefault(entryId);

            if (type == InventoryType.Contraband && component.Contraband)
                return component.ContrabandInventory.GetValueOrDefault(entryId);

            return component.Inventory.GetValueOrDefault(entryId);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<VendingMachineComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.Ejecting)
                {
                    comp.EjectAccumulator += frameTime;
                    if (comp.EjectAccumulator >= comp.EjectDelay)
                    {
                        comp.EjectAccumulator = 0f;
                        comp.Ejecting = false;

                        EjectItem(uid, comp.NextItemCount, comp);   // ADT vending eject count
                    }
                }

                if (comp.Denying)
                {
                    comp.DenyAccumulator += frameTime;
                    if (comp.DenyAccumulator >= comp.DenyDelay)
                    {
                        comp.DenyAccumulator = 0f;
                        comp.Denying = false;

                        TryUpdateVisualState(uid, comp);
                    }
                }

                if (comp.DispenseOnHitCoolingDown)
                {
                    comp.DispenseOnHitAccumulator += frameTime;
                    if (comp.DispenseOnHitAccumulator >= comp.DispenseOnHitCooldown)
                    {
                        comp.DispenseOnHitAccumulator = 0f;
                        comp.DispenseOnHitCoolingDown = false;
                    }
                }
            }
            var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent>();
            while (disabled.MoveNext(out var uid, out _, out var comp))
            {
                if (comp.NextEmpEject < _timing.CurTime)
                {
                    EjectRandom(uid, true, false, comp);
                    comp.NextEmpEject += TimeSpan.FromSeconds(5 * comp.EjectDelay);
                }
            }
        }

        public void TryRestockInventory(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            RestockInventoryFromPrototype(uid, vendComponent);

            Dirty(uid, vendComponent);
            TryUpdateVisualState(uid, vendComponent);
        }

        private void OnPriceCalculation(EntityUid uid, VendingMachineRestockComponent component, ref PriceCalculationEvent args)
        {
            List<double> priceSets = new();

            // Find the most expensive inventory and use that as the highest price.
            foreach (var vendingInventory in component.CanRestock)
            {
                double total = 0;

                if (PrototypeManager.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
                {
                    foreach (var (item, amount) in inventoryPrototype.StartingInventory)
                    {
                        if (PrototypeManager.TryIndex(item, out EntityPrototype? entity))
                            total += _pricing.GetEstimatedPrice(entity) * amount;
                    }
                }

                priceSets.Add(total);
            }

            args.Price += priceSets.Max();
        }

        private void OnEmpPulse(EntityUid uid, VendingMachineComponent component, ref EmpPulseEvent args)
        {
            if (!component.Broken && this.IsPowered(uid, EntityManager))
            {
                args.Affected = true;
                args.Disabled = true;
                component.NextEmpEject = _timing.CurTime;
            }
        }
    }
}
