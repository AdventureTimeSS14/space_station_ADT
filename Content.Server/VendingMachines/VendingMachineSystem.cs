using System.Linq;
using System.Numerics;
using Content.Server.Advertise.EntitySystems;
using Content.Server.ADT.Economy;
using Content.Server.ADT.VendingMachines;
using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Advertise.Components;
using Content.Shared.ADT.Economy;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Store.Components;

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly SpeakOnUIClosedSystem _speakOnUIClosed = default!;
        //ADT-Economy-Start
        [Dependency] private readonly BankCardSystem _bankCard = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly ADTVendingMachineReturnSystem _vendingReturn = default!;
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        //ADT-Economy-End
        [Dependency] private readonly SharedPointLightSystem _light = default!;
        [Dependency] private readonly EmagSystem _emag = default!;

        private const float WallVendEjectDistanceFromWall = 1f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage); //ADT-Economy
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineComponent, TryVocalizeEvent>(OnTryVocalize);

            Subs.BuiEvents<VendingMachineComponent>(VendingMachineUiKey.Key, subs =>
            {
                subs.Event<VendingMachineEjectMessage>(OnInventoryEjectMessage);
                subs.Event<VendingMachineEjectCountMessage>(OnInventoryEjectCountMessage);  // ADT vending eject count
            });

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            //ADT-Economy-Start
            SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineWithdrawMessage>(OnWithdrawMessage);
            //ADT-Economy-End
            // ADT-Tweak start
            SubscribeLocalEvent<VendingMachineComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
            // ADT-Tweak end

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

        private void OnDamage(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args) //ADT-Economy
        {
            if (!args.DamageIncreased && component.Broken)
            {
                component.Broken = false;
                Dirty(uid, component);
                TryUpdateVisualState(uid, component);
                return;
            }

            if (component.Broken || component.DispenseOnHitCoolingDown ||
                component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageIncreased && args.DamageDelta.GetTotal() >= component.DispenseOnHitThreshold &&
                _random.Prob(component.DispenseOnHitChance.Value))
            {
                if (component.DispenseOnHitCooldown > 0f)
                {
                    component.DispenseOnHitCoolingDown = true;
                    component.DispenseOnHitEnd = Timing.CurTime + TimeSpan.FromSeconds(component.DispenseOnHitCooldown.Value);
                }
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

            if (HasComp<ToolComponent>(args.Used))
                return;

            if (!TryComp<CurrencyComponent>(args.Used, out var currency) ||
                !currency.Price.Keys.Contains(component.CurrencyType))

            {
                if (_vendingReturn.TryReturnItem(uid, component, args.User, args.Used))
                    args.Handled = true;
                return;
            }

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
            _stackSystem.SpawnAtPosition(component.Credits, component.CreditStackPrototype, // ADT-Fix
                Transform(uid).Coordinates);

            component.Credits = 0;
            Audio.PlayPvs(component.SoundWithdrawCurrency, uid);

            UpdateVendingMachineInterfaceState(uid, component);
        }

        private void OnAfterActivatableUIOpen(EntityUid uid, VendingMachineComponent component, AfterActivatableUIOpenEvent args)
        {
            SendUserInfo(uid, args.User);
        }

        private void SendUserInfo(EntityUid uid, EntityUid user)
        {
            var balance = 0;

            if (IsCargoAccountUser(user) &&
                _stationSystem.GetOwningStation(user) is { } station &&
                TryComp<StationBankAccountComponent>(station, out var stationBank))
            {
                balance = _cargoSystem.GetBalanceFromAccount((station, stationBank), stationBank.PrimaryAccount);
                _userInterfaceSystem.ServerSendUiMessage(uid, VendingMachineUiKey.Key,
                    new VendingMachineUserInfoMessage(balance), user);
                return;
            }

            var items = _accessReader.FindPotentialAccessItems(user);
            foreach (var item in items)
            {
                var nextItem = item;
                if (TryComp(item, out PdaComponent? pda) && pda.ContainedId is { Valid: true } id)
                    nextItem = id;

                if (TryComp<BankCardComponent>(nextItem, out var bankCard) && bankCard.AccountId.HasValue)
                {
                    balance = _bankCard.GetBalance(bankCard.AccountId.Value);
                    break;
                }
            }

            _userInterfaceSystem.ServerSendUiMessage(uid, VendingMachineUiKey.Key,
                new VendingMachineUserInfoMessage(balance, IsBalanceExempt(user)), user);
        }

        private bool IsBalanceExempt(EntityUid user)
        {
            return _tag.HasTag(user, "IgnoreBalanceChecks");
        }

        private bool IsCargoAccountUser(EntityUid user)
        {
            return _tag.HasTag(user, "ADTVendingCargoAccount");
        }

        private void OnInventoryEjectCountMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectCountMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Entry.Type, args.Entry.ID, component, args.Count, args.PaintColor); // ADT-Tweak
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

            vendComponent.DenyEnd = Timing.CurTime + vendComponent.DenyDelay;
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
        public override bool IsAuthorized(EntityUid uid, EntityUid sender, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
                return true;

            // ADT-tweak: Emagged vending machines should allow everyone
            if (_emag.CheckFlag(uid, EmagType.Interaction))
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
        // ADT: This overloads the Shared method because we need 'count' and 'sender' parameters for economy
        public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, int count, VendingMachineComponent? vendComponent = null, EntityUid? sender = null, Color? paintColor = null) // ADT-Tweak
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

            //ADT-Economy-Start
            var returnedCount = (int)vendComponent.ReturnedInventory.GetValueOrDefault(itemId);
            if (count <= 0 || count > (int)entry.Amount + returnedCount)
            {
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, sender.Value);

                Deny(uid, vendComponent);
                return;
            }
            
            if (string.IsNullOrEmpty(entry.ID))
                return;

            var freeCount = Math.Min(returnedCount, count);
            var price = GetPrice(entry, vendComponent, count - freeCount);
            if (price > 0 && !vendComponent.AllForFree && sender.HasValue && !IsBalanceExempt(sender.Value))
            {
                var success = false;

                if (IsCargoAccountUser(sender.Value) &&
                    _stationSystem.GetOwningStation(sender.Value) is { } station &&
                    TryComp<StationBankAccountComponent>(station, out var stationBank))
                {
                    success = _cargoSystem.GetBalanceFromAccount((station, stationBank), stationBank.PrimaryAccount) >= price;
                    if (success)
                        _cargoSystem.UpdateBankAccount((station, stationBank), -price, stationBank.PrimaryAccount);
                }
                else
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
            vendComponent.NextItemReturnedCount = freeCount; //ADT-Return
            vendComponent.NextItemPaintColor = paintColor; // ADT-Tweak
            //ADT-Economy-End

            // Start Ejecting, and prevent users from ordering while anim playing
            // Upstream adapted: use timestamp-based approach
            vendComponent.EjectEnd = Timing.CurTime + vendComponent.EjectDelay;
            vendComponent.Ejecting = true;
            vendComponent.NextItemToEject = entry.ID;
            vendComponent.ThrowNextItem = throwItem;

            if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
                _speakOnUIClosed.TrySetFlag((uid, speakComponent));

            //ADT-Return start
            entry.Amount = (uint)Math.Max(0, (int)entry.Amount - (count - freeCount));
            if (freeCount > 0)
            {
                var left = returnedCount - freeCount;
                if (left > 0)
                    vendComponent.ReturnedInventory[itemId] = (uint)left;
                else
                    vendComponent.ReturnedInventory.Remove(itemId);
            }
            //ADT-Return end
            Dirty(uid, vendComponent);
            UpdateVendingMachineInterfaceState(uid, vendComponent); // // ADT-Tweak
            TryUpdateVisualState(uid, vendComponent);
            Audio.PlayPvs(vendComponent.SoundVend, uid);

            // ADT-Tweak start
            if (sender.HasValue)
                SendUserInfo(uid, sender.Value);
            // ADT-Tweak end
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="component"></param>
        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component, int count, Color? paintColor = null)  // ADT-Tweak
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, type, itemId, component.CanShoot, count, component, sender, paintColor); // ADT-Tweak
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
                // ADT-Tweak start
                if (vendComponent.Ejecting)
                    return;
                // ADT-Tweak end

                vendComponent.NextItemToEject = item.ID;
                vendComponent.ThrowNextItem = throwItem;
                vendComponent.NextItemCount = 1;
                vendComponent.NextItemPaintColor = null; // ADT-Tweak
                //ADT-Return start
                var returnedCount = (int)vendComponent.ReturnedInventory.GetValueOrDefault(item.ID);
                var freeCount = Math.Min(returnedCount, 1);
                vendComponent.NextItemReturnedCount = freeCount;
                var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
                if (entry != null)
                    entry.Amount = (uint)Math.Max(0, (int)entry.Amount - (1 - freeCount));
                if (freeCount > 0)
                {
                    if (returnedCount > 1)
                        vendComponent.ReturnedInventory[item.ID] = (uint)(returnedCount - 1);
                    else
                        vendComponent.ReturnedInventory.Remove(item.ID);
                }
                //ADT-Return end
                EjectItem(uid, vendComponent, forceEject);   // ADT vending eject count
            }
            else
            {
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, 1, vendComponent);   // ADT vending eject count
            }
        }

        protected override void EjectItem(EntityUid uid, VendingMachineComponent? vendComponent = null, bool forceEject = false)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var count = vendComponent.NextItemCount;

            if (string.IsNullOrEmpty(vendComponent.NextItemToEject))
            {
                vendComponent.ThrowNextItem = false;
                return;
            }

            // Default spawn coordinates
            var xform = Transform(uid);
            var spawnCoordinates = xform.Coordinates;

            //Make sure the wallvends spawn outside of the wall.
            if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
            {
                var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * WallVendEjectDistanceFromWall;
                spawnCoordinates = spawnCoordinates.Offset(offset);
            }
            // ADT-Return start
            var returnedCount = vendComponent.NextItemReturnedCount;
            if (returnedCount > 0)
            {
                RaiseLocalEvent(uid, new ADTVendingReturnedEjectEvent(
                    vendComponent.NextItemToEject, returnedCount, spawnCoordinates, vendComponent.ThrowNextItem, vendComponent.NextItemPaintColor)); // ADT Tweak - цвет
            }
            // ADT-Return end

            // ADT vending eject count start
            for (var i = 0; i < count - returnedCount; i++) // ADT-Return 
            {
                var ent = Spawn(vendComponent.NextItemToEject, spawnCoordinates);

                if (vendComponent.NextItemPaintColor is { } paintColor)
                    _vendingReturn.PaintClothing(ent, paintColor);

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
            vendComponent.NextItemReturnedCount = 0;    //ADT-Return
            vendComponent.NextItemPaintColor = null; // ADT-Tweak
            vendComponent.Ejecting = false;     // ADT-Tweak

            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                TryUpdateVisualState(uid, vendComponent);

            UpdateVendingMachineInterfaceState(uid, vendComponent); // ADT-Tweak
        }

        protected override VendingMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, InventoryType type, VendingMachineComponent? component = null)
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

            var curTime = Timing.CurTime;

            var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent>();
            while (disabled.MoveNext(out var uid, out _, out var comp))
            {
                if (comp.NextEmpEject < curTime)
                {
                    EjectRandom(uid, true, false, comp);
                    comp.NextEmpEject += TimeSpan.FromSeconds(5 * comp.EjectDelay.TotalSeconds);
                }
            }
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
                    foreach (var (item, amount, _) in VendingMachineInventoryData.Flatten(inventoryPrototype.StartingInventory)) // ADT-Twek
                    {
                        if (PrototypeManager.TryIndex(item, out EntityPrototype? entity))
                            total += _pricing.GetEstimatedPrice(entity) * amount;
                    }
                }

                priceSets.Add(total);
            }

            args.Price += priceSets.Max();
        }

        private void OnTryVocalize(Entity<VendingMachineComponent> ent, ref TryVocalizeEvent args)
        {
            // ADT-Tweak start - я не могу опнять где она не инцелизируется по этому будет тут
            if (!TryComp<MetaDataComponent>(ent.Owner, out var meta) || !meta.EntityInitialized)
                return;
            // ADT-Tweak end

            if (ent.Comp.Broken)
                args.Cancelled = true;
        }
    }
}
