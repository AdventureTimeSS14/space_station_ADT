using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Advertise.Components;
using Content.Shared.Advertise.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.VendingMachines;

public abstract partial class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IPrototypeManager PrototypeManager = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected IRobustRandom Randomizer = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected SharedUserInterfaceSystem UISystem = default!;
    [Dependency] private SharedSpeakOnUIClosedSystem _speakOnUIClosed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTVendingMachineComponent, ComponentGetState>(OnVendingGetState);
        SubscribeLocalEvent<ADTVendingMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTVendingMachineComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<ADTVendingMachineComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ADTVendingMachineComponent, RestockDoAfterEvent>(OnRestockDoAfter);
        SubscribeLocalEvent<ADTVendingMachineComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<ADTVendingMachineComponent, BreakageEventArgs>(OnBreak);

        SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnVendingGetState(Entity<ADTVendingMachineComponent> entity, ref ComponentGetState args)
    {
        var component = entity.Comp;
        var state = new VendingMachineComponentState
        {
            Contraband = component.Contraband,
            Broken = component.Broken,
            ReturnedInventory = new(component.ReturnedInventory),
        };

        CopyInventory(component.Inventory, state.Inventory);
        CopyInventory(component.EmaggedInventory, state.EmaggedInventory);
        CopyInventory(component.ContrabandInventory, state.ContrabandInventory);

        args.State = state;
    }

    protected static void CopyInventory(
        Dictionary<string, VendingMachineInventoryEntry> source,
        Dictionary<string, VendingMachineInventoryEntry> target)
    {
        target.Clear();

        foreach (var entry in source)
        {
            target.Add(entry.Key, new VendingMachineInventoryEntry(entry.Value));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTVendingMachineComponent, VendingMachineEjectComponent>();
        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var eject))
        {
            UpdateEjectState((uid, comp, eject), curTime);
        }
    }

    private void UpdateEjectState(Entity<ADTVendingMachineComponent, VendingMachineEjectComponent> entity, TimeSpan curTime)
    {
        var eject = entity.Comp2;
        if (eject.EjectEnd is { } ejectEnd && curTime > ejectEnd)
        {
            eject.EjectEnd = null;
            Dirty(entity.Owner, eject);

            EjectItem((entity.Owner, entity.Comp1, eject));
            UpdateUI((entity.Owner, entity.Comp1));
            OnEjectStateChanged((entity.Owner, entity.Comp1), eject);
        }

        if (eject.DenyEnd is not { } denyEnd || curTime <= denyEnd)
            return;

        eject.DenyEnd = null;
        Dirty(entity.Owner, eject);

        OnEjectStateChanged((entity.Owner, entity.Comp1), eject);
    }

    private void OnEmpPulse(Entity<ADTVendingMachineComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Broken || !_receiver.IsPowered(ent.Owner))
            return;

        if (!TryComp<VendingMachineEjectComponent>(ent.Owner, out var eject))
            return;

        args.Affected = true;
        args.Disabled = true;
        eject.NextEmpEject = Timing.CurTime;
    }

    protected virtual void OnMapInit(EntityUid uid, ADTVendingMachineComponent component, MapInitEvent args)
    {
        RestockInventoryFromPrototype(uid, component, component.InitialStockQuality);
    }

    protected virtual void EjectItem(Entity<ADTVendingMachineComponent?, VendingMachineEjectComponent?> entity, bool forceEject = false) { }

    protected virtual void OnEjectStateChanged(Entity<ADTVendingMachineComponent?> entity, VendingMachineEjectComponent? ejectComponent = null) { }

    protected virtual bool ShouldThrowVendItem(Entity<VendingMachineEjectComponent> entity)
    {
        return false;
    }

    /// <summary>
    /// Checks if the user is authorized to use this vending machine
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="sender">Entity trying to use the vending machine</param>
    /// <param name="vendComponent"></param>
    public virtual bool IsAuthorized(EntityUid uid, EntityUid sender, ADTVendingMachineComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return false;

        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return true;

        if (_accessReader.IsAllowed(sender, uid, accessReader) || HasComp<EmaggedComponent>(uid))
            return true;

        Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender);
        Deny((uid, vendComponent), sender);
        return false;
    }

    protected virtual VendingMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, InventoryType type, ADTVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (type == InventoryType.Emagged && HasComp<EmaggedComponent>(uid))
            return component.EmaggedInventory.GetValueOrDefault(entryId);

        if (type == InventoryType.Contraband && component.Contraband)
            return component.ContrabandInventory.GetValueOrDefault(entryId);

        return component.Inventory.GetValueOrDefault(entryId);
    }

    /// <summary>
    /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
    /// or the item doesn't exist in its inventory.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="type">The type of inventory the item is from</param>
    /// <param name="itemId">The prototype ID of the item</param>
    /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
    /// <param name="user"></param>
    /// <param name="vendComponent"></param>
    /// <param name="ejectComponent"></param>
    public void TryEjectVendorItem(
        EntityUid uid,
        InventoryType type,
        string itemId,
        bool throwItem,
        EntityUid? user = null,
        ADTVendingMachineComponent? vendComponent = null,
        VendingMachineEjectComponent? ejectComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return;

        if (!Resolve(uid, ref ejectComponent))
            return;

        if (ejectComponent.Ejecting || vendComponent.Broken || !_receiver.IsPowered(uid))
        {
            return;
        }

        var entry = GetEntry(uid, itemId, type, vendComponent);

        if (entry == null)
        {
            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, uid);
            Deny((uid, vendComponent), ejectComponent: ejectComponent);
            return;
        }

        if (entry.Amount <= 0)
        {
            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, uid);
            Deny((uid, vendComponent), ejectComponent: ejectComponent);
            return;
        }

        // Start Ejecting and prevent users from ordering while anim playing
        ejectComponent.EjectEnd = Timing.CurTime + ejectComponent.EjectDelay;
        ejectComponent.NextItemToEject = entry.ID;
        ejectComponent.ThrowNextItem = throwItem;

        if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
            _speakOnUIClosed.TrySetFlag((uid, speakComponent));

        entry.Amount--;
        Dirty(uid, vendComponent);
        Dirty(uid, ejectComponent);
        UpdateUI((uid, vendComponent));
        OnEjectStateChanged((uid, vendComponent), ejectComponent);
        Audio.PlayPredicted(ejectComponent.SoundVend, uid, user);
    }

    public void Deny(Entity<ADTVendingMachineComponent?> entity, EntityUid? user = null, VendingMachineEjectComponent? ejectComponent = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (!Resolve(entity.Owner, ref ejectComponent))
            return;

        if (ejectComponent.Denying)
            return;

        ejectComponent.DenyEnd = Timing.CurTime + ejectComponent.DenyDelay;
        var audioParams = ejectComponent.SoundDeny?.Params ?? AudioParams.Default;
        audioParams = audioParams.AddVolume(-2f);
        Audio.PlayPredicted(ejectComponent.SoundDeny, entity.Owner, user, audioParams);
        OnEjectStateChanged(entity, ejectComponent);
        Dirty(entity.Owner, ejectComponent);
    }

    /// <summary>
    /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="sender">Entity that is trying to use the vending machine</param>
    /// <param name="type">The type of inventory the item is from</param>
    /// <param name="itemId">The prototype ID of the item</param>
    /// <param name="component"></param>
    public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, ADTVendingMachineComponent component)
    {
        if (!IsAuthorized(uid, sender, component))
            return;

        if (!TryComp<VendingMachineEjectComponent>(uid, out var ejectComponent))
            return;

        TryEjectVendorItem(uid, type, itemId, ShouldThrowVendItem((uid, ejectComponent)), sender, component, ejectComponent);
    }

    protected virtual void UpdateUI(Entity<ADTVendingMachineComponent?> entity) { }

    public void RestockInventoryFromPrototype(EntityUid uid,
        ADTVendingMachineComponent? component = null, float restockQuality = 1f)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!PrototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            return;

        AddInventoryFromPrototype(uid, VendingMachineInventoryData.Flatten(packPrototype.StartingInventory), InventoryType.Regular, component, restockQuality); // ADT-Tweak
        AddInventoryFromPrototype(uid, VendingMachineInventoryData.Flatten(packPrototype.EmaggedInventory), InventoryType.Emagged, component, restockQuality); // ADT-Tweak
        AddInventoryFromPrototype(uid, VendingMachineInventoryData.Flatten(packPrototype.ContrabandInventory), InventoryType.Contraband, component, restockQuality); // ADT-Tweak
        Dirty(uid, component);
    }

    private void OnEmagged(EntityUid uid, ADTVendingMachineComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        // only emag if there are emag-only items
        args.Handled = component.EmaggedInventory.Count > 0 || component.PriceMultiplier > 0; // ADT-Economy

        // ADT-tweak start
        if (args.Handled)
        {
            // Make all items free when emagged
            component.AllForFree = true;
            Dirty(uid, component);
        }
        // ADT-tweak end
    }

    /// <summary>
    /// Returns all of the vending machine's inventory. Only includes emagged and contraband inventories if
    /// <see cref="EmaggedComponent"/> with the EmagType.Interaction flag exists and <see cref="ADTVendingMachineComponent.Contraband"/> is true
    /// are <c>true</c> respectively.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, ADTVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        var inventory = new List<VendingMachineInventoryEntry>(component.Inventory.Values);

        // ADT-Return start
        var mergedInventory = new List<VendingMachineInventoryEntry>(component.Inventory.Values.Count);
        foreach (var entry in component.Inventory.Values)
        {
            if (!component.ReturnedInventory.TryGetValue(entry.ID, out var returnedAmount))
            {
                mergedInventory.Add(entry);
                continue;
            }

            mergedInventory.Add(new VendingMachineInventoryEntry(entry.Type, entry.ID,
                entry.Amount + returnedAmount,
                returnedAmount > 0 ? 0 : entry.Price,
                entry.MaxAmount + returnedAmount, entry.Category));
        }
        inventory = mergedInventory;
        // ADT-Return end

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            inventory.AddRange(component.EmaggedInventory.Values);

        if (component.Contraband)
            inventory.AddRange(component.ContrabandInventory.Values);

        return inventory;
    }

    public List<VendingMachineInventoryEntry> GetAvailableInventory(EntityUid uid, ADTVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        return GetAllInventory(uid, component).Where(inventoryEntry => inventoryEntry.Amount > 0).ToList(); // ADT-Economy
    }

    private void AddInventoryFromPrototype(EntityUid uid, IEnumerable<(string Id, uint Amount, string? Category)> entries, // ADT-Tweak
        InventoryType type,
        ADTVendingMachineComponent? component = null, float restockQuality = 1.0f)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        Dictionary<string, VendingMachineInventoryEntry> inventory;
        switch (type)
        {
            case InventoryType.Regular:
                inventory = component.Inventory;
                break;
            case InventoryType.Emagged:
                inventory = component.EmaggedInventory;
                break;
            case InventoryType.Contraband:
                inventory = component.ContrabandInventory;
                break;
            default:
                return;
        }

        foreach (var (id, amount, category) in entries) // ADT-Tweak
        {
            if (PrototypeManager.TryIndex<EntityPrototype>(id, out var proto)) // ADT-Economy
            {
                var restock = amount;
                var chanceOfMissingStock = 1 - restockQuality;

                var result = Randomizer.NextFloat(0, 1);
                if (result < chanceOfMissingStock)
                {
                    restock = (uint) Math.Floor(amount * result / chanceOfMissingStock);
                }

                if (inventory.TryGetValue(id, out var entry))
                    // Prevent a machine's stock from going over three times
                    // the prototype's normal amount. This is an arbitrary
                    // number and meant to be a convenience for someone
                    // restocking a machine who doesn't want to force vend out
                    // all the items just to restock one empty slot without
                    // losing the rest of the restock.

                //ADT-Economy-Start
                    entry.Amount = Math.Max(entry.Amount, Math.Min(entry.Amount + restock, 3 * amount));
                else
                {
                    var price = GetEntryPrice(proto);
                    inventory.Add(id, new VendingMachineInventoryEntry(type, id, restock, price, amount, category));
                }
                //ADT-Economy-End
            }
        }
    }

    //ADT-Economy-Start
    protected virtual int GetEntryPrice(EntityPrototype proto)
    {
        return 25;
    }
    //ADT-Economy-End

    private void OnActivatableUIOpenAttempt(EntityUid uid, ADTVendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (component.Broken)
            args.Cancel();
    }

    private void OnBreak(EntityUid uid, ADTVendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
    {
        vendComponent.Broken = true;
        Dirty(uid, vendComponent);

        UISystem.CloseUi(uid, ADTVendingMachineUiKey.Key);
    }
}