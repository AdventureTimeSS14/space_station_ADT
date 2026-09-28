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
using Content.Server.VendingMachines.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Advertise.Components;
using Content.Shared.ADT.Economy;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines.Components;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Store.Components;
using VendingMachineComponent = Content.Shared.ADT.VendingMachines.VendingMachineComponent;

namespace Content.Server.ADT.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly SpeakOnUIClosedSystem _speakOnUIClosed = default!;
        [Dependency] private readonly BankCardSystem _bankCard = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly ADTVendingMachineReturnSystem _vendingReturn = default!;
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly LabelSystem _label = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

        private const float WallVendEjectDistanceFromWall = 1f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineComponent, TryVocalizeEvent>(OnTryVocalize);

            Subs.BuiEvents<VendingMachineComponent>(VendingMachineUiKey.Key, subs =>
            {
                subs.Event<VendingMachineEjectMessage>(OnInventoryEjectMessage);
                subs.Event<VendingMachineEjectCountMessage>(OnInventoryEjectCountMessage);
            });

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineWithdrawMessage>(OnWithdrawMessage);
            SubscribeLocalEvent<VendingMachineComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);

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

        public void UpdateVendingMachineInterfaceState(EntityUid uid, VendingMachineComponent component)
        {
            var state = new VendingMachineInterfaceState(GetAllInventory(uid, component), component.PriceMultiplier,
                component.Credits, BuildReturnedItemDisplays(uid, component));

            _userInterfaceSystem.SetUiState(uid, VendingMachineUiKey.Key, state);
        }

        private Dictionary<string, ReturnedItemDisplay> BuildReturnedItemDisplays(EntityUid uid, VendingMachineComponent component)
        {
            var result = new Dictionary<string, ReturnedItemDisplay>();

            if (!EntityManager.TryGetComponent(uid, out ContainerManagerComponent? containers)
                || !containers.Containers.TryGetValue(VendingMachineComponent.ReturnedItemsContainerId, out var container))
            {
                return result;
            }

            var seen = new HashSet<string>();
            for (var i = container.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var ent = container.ContainedEntities[i];
                if (!TryComp<MetaDataComponent>(ent, out var meta) || meta.EntityPrototype?.ID is not { } protoId)
                    continue;

                if (!seen.Add(protoId))
                    continue;

                result[protoId] = BuildReturnedItemDisplay(ent);
            }

            return result;
        }

        private ReturnedItemDisplay BuildReturnedItemDisplay(EntityUid ent)
        {
            var display = new ReturnedItemDisplay
            {
                Label = _label.GetLabelText(ent),
            };

            string? solutionName = null;
            if (TryComp<SolutionContainerVisualsComponent>(ent, out var visuals))
                solutionName = visuals.SolutionName;

            if (_solutionContainer.TryGetSolution(ent, solutionName, out _, out var solution)
                && solution.Volume > FixedPoint2.Zero
                && solution.MaxVolume > FixedPoint2.Zero)
            {
                display.FillFraction = solution.FillFraction;
                if (visuals is { ChangeColor: true })
                    display.FillColor = solution.GetColor(PrototypeManager);
            }

            return display;
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Type, args.ID, component, 1);
        }

        private void OnDamage(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased && component.Broken)
            {
                component.Broken = false;
                Dirty(uid, component);
                return;
            }

            if (!TryComp<VendingMachineDispenseOnHitComponent>(uid, out var dispenseOnHit))
                return;

            if (component.Broken || dispenseOnHit.CoolingDown || args.DamageDelta == null)
                return;

            if (!(args.DamageIncreased && args.DamageDelta.GetTotal() >= dispenseOnHit.Threshold) ||
                !_random.Prob(dispenseOnHit.Chance)) return;

            if (dispenseOnHit.NextDispenseDelay != null)
            {
                dispenseOnHit.NextDispenseTime = Timing.CurTime + dispenseOnHit.NextDispenseDelay.Value;
            }

            if (!TryComp<VendingMachineEjectComponent>(uid, out var eject))
                return;

            EjectRandom((uid, component, eject), throwItem: true, forceEject: true);
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (!TryComp<VendingMachineEjectComponent>(uid, out var eject))
                return;

            EjectRandom((uid, component, eject), throwItem: true, forceEject: false);
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
            _stackSystem.SpawnAtPosition(component.Credits, component.CreditStackPrototype,
                Transform(uid).Coordinates);

            component.Credits = 0;
            Audio.PlayPvs(component.SoundWithdrawCurrency, uid);

            UpdateVendingMachineInterfaceState(uid, component);
        }

        private void OnAfterActivatableUIOpen(EntityUid uid, VendingMachineComponent component, AfterActivatableUIOpenEvent args)
        {
            SendUserInfo(uid, args.User);
            UpdateVendingMachineInterfaceState(uid, component);
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

            AuthorizedVend(uid, entity, args.Entry.Type, args.Entry.ID, component, args.Count, args.PaintColor);
        }

        /// <summary>
        /// Sets the shooting state of the vending machine (adds or removes <see cref="VendingMachineShootComponent"/>).
        /// </summary>
        public void SetShooting(Entity<VendingMachineEjectComponent?> entity, bool canShoot)
        {
            if (!Resolve(entity.Owner, ref entity.Comp))
                return;

            if (canShoot)
                EnsureComp<VendingMachineShootComponent>(entity.Owner);
            else
                RemComp<VendingMachineShootComponent>(entity.Owner);
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.Contraband"/> property of the vending machine.
        /// </summary>
        public void SetContraband(Entity<VendingMachineComponent> entity, bool contraband)
        {
            entity.Comp.Contraband = contraband;
            Dirty(entity);
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

            if (_emag.CheckFlag(uid, EmagType.Interaction))
                return true;

            if (_accessReader.IsAllowed(sender, uid, accessReader))
                return true;

            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender);
            Deny((uid, vendComponent));
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
        public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, int count, VendingMachineComponent? vendComponent = null, EntityUid? sender = null, Color? paintColor = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (!Resolve(uid, out VendingMachineEjectComponent? ejectComponent))
                return;

            if (ejectComponent.Ejecting || vendComponent.Broken || !this.IsPowered(uid, EntityManager))
            {
                return;
            }

            var entry = GetEntry(uid, itemId, type, vendComponent);

            if (entry == null)
            {
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, sender.Value);

                Deny((uid, vendComponent), ejectComponent: ejectComponent);
                return;
            }

            var returnedCount = (int)vendComponent.ReturnedInventory.GetValueOrDefault(itemId);
            if (count <= 0 || count > (int)entry.Amount + returnedCount)
            {
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, sender.Value);

                Deny((uid, vendComponent), ejectComponent: ejectComponent);
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
                    Deny((uid, vendComponent), ejectComponent: ejectComponent);
                    return;
                }
            }
            vendComponent.NextItemCount = count;
            vendComponent.NextItemReturnedCount = freeCount;
            vendComponent.NextItemPaintColor = paintColor;

            // Start Ejecting, and prevent users from ordering while anim playing
            ejectComponent.EjectEnd = Timing.CurTime + ejectComponent.EjectDelay;
            ejectComponent.NextItemToEject = entry.ID;
            ejectComponent.ThrowNextItem = throwItem;

            if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
                _speakOnUIClosed.TrySetFlag((uid, speakComponent));

            entry.Amount = (uint)Math.Max(0, (int)entry.Amount - (count - freeCount));
            if (freeCount > 0)
            {
                var left = returnedCount - freeCount;
                if (left > 0)
                    vendComponent.ReturnedInventory[itemId] = (uint)left;
                else
                    vendComponent.ReturnedInventory.Remove(itemId);
            }

            Dirty(uid, vendComponent);
            Dirty(uid, ejectComponent);
            UpdateVendingMachineInterfaceState(uid, vendComponent);
            OnEjectStateChanged((uid, vendComponent), ejectComponent);
            Audio.PlayPvs(ejectComponent.SoundVend, uid);

            if (sender.HasValue)
                SendUserInfo(uid, sender.Value);
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="component"></param>
        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component, int count, Color? paintColor = null)
        {
            if (IsAuthorized(uid, sender, component))
            {
                var canShoot = HasComp<VendingMachineShootComponent>(uid);
                TryEjectVendorItem(uid, type, itemId, canShoot, count, component, sender, paintColor);
            }
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        public void EjectRandom(
            Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity,
            bool throwItem,
            bool forceEject = false)
        {
            if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2))
                return;

            var uid = entity.Owner;
            var vendComponent = entity.Comp1;
            var ejectComponent = entity.Comp2;
            var availableItems = GetAvailableInventory(uid, vendComponent);
            if (availableItems.Count <= 0)
                return;

            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                if (ejectComponent.Ejecting)
                    return;

                ejectComponent.NextItemToEject = item.ID;
                ejectComponent.ThrowNextItem = throwItem;
                vendComponent.NextItemCount = 1;
                vendComponent.NextItemPaintColor = null;

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
                EjectItem((uid, vendComponent, ejectComponent), forceEject);
            }
            else
            {
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, 1, vendComponent);
            }
        }

        protected override void EjectItem(Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity, bool forceEject = false)
        {
            if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2))
                return;

            var uid = entity.Owner;
            var vendComponent = entity.Comp1;
            var ejectComponent = entity.Comp2;
            var count = vendComponent.NextItemCount;

            if (string.IsNullOrEmpty(ejectComponent.NextItemToEject))
            {
                ejectComponent.ThrowNextItem = false;
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
            var returnedCount = vendComponent.NextItemReturnedCount;
            if (returnedCount > 0)
            {
                RaiseLocalEvent(uid, new ADTVendingReturnedEjectEvent(
                    ejectComponent.NextItemToEject, returnedCount, spawnCoordinates, ejectComponent.ThrowNextItem, vendComponent.NextItemPaintColor));
            }

            for (var i = 0; i < count - returnedCount; i++)
            {
                var ent = Spawn(ejectComponent.NextItemToEject, spawnCoordinates);

                if (vendComponent.NextItemPaintColor is { } paintColor)
                    _vendingReturn.PaintClothing(ent, paintColor);

                if (ejectComponent.ThrowNextItem)
                {
                    var range = ejectComponent.NonLimitedEjectRange;
                    var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                    _throwingSystem.TryThrow(ent, direction, ejectComponent.NonLimitedEjectForce);
                }
            }

            ejectComponent.NextItemToEject = null;
            ejectComponent.ThrowNextItem = false;
            vendComponent.NextItemCount = 1;
            vendComponent.NextItemReturnedCount = 0;
            vendComponent.NextItemPaintColor = null;

            OnEjectStateChanged((uid, vendComponent), ejectComponent);

            UpdateVendingMachineInterfaceState(uid, vendComponent);
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

            var dispenseOnHitQuery = EntityQueryEnumerator<VendingMachineDispenseOnHitComponent>();
            while (dispenseOnHitQuery.MoveNext(out _, out var dispenseOnHit))
            {
                if (dispenseOnHit.NextDispenseTime is not { } nextDispenseTime || curTime <= nextDispenseTime)
                    continue;

                dispenseOnHit.NextDispenseTime = null;
            }

            var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent, VendingMachineEjectComponent>();
            while (disabled.MoveNext(out var uid, out _, out var comp, out var eject))
            {
                if (eject.NextEmpEject < curTime)
                {
                    EjectRandom((uid, comp, eject), true, false);
                    eject.NextEmpEject += TimeSpan.FromSeconds(5 * eject.EjectDelay.TotalSeconds);
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
                    foreach (var (item, amount, _) in VendingMachineInventoryData.Flatten(inventoryPrototype.StartingInventory))
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
            if (!TryComp<MetaDataComponent>(ent.Owner, out var meta) || !meta.EntityInitialized)
                return;

            if (ent.Comp.Broken)
                args.Cancelled = true;
        }
    }
}