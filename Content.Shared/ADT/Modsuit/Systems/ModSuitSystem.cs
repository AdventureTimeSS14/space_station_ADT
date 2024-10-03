using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Wires;

namespace Content.Shared.ADT.ModSuits;

public sealed class ModSuitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitComponent, ComponentInit>(OnMODSuitInit);
        SubscribeLocalEvent<ModSuitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModSuitComponent, ToggleClothingEvent>(OnToggleMODPartAction);
        SubscribeLocalEvent<ModSuitComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ModSuitComponent, ComponentRemove>(OnRemoveModsuit);
        SubscribeLocalEvent<ModSuitComponent, GotUnequippedEvent>(OnModsuitUnequip);
        SubscribeLocalEvent<ModSuitComponent, ModSuitUiMessage>(OnToggleClothingMessage);
        SubscribeLocalEvent<ModSuitComponent, BeingUnequippedAttemptEvent>(OnModsuitUnequipAttempt);

        SubscribeLocalEvent<ModPartComponent, ComponentInit>(OnAttachedInit);
        SubscribeLocalEvent<ModPartComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ModPartComponent, GotUnequippedEvent>(OnAttachedUnequip);
        SubscribeLocalEvent<ModPartComponent, ComponentRemove>(OnRemoveAttached);
        SubscribeLocalEvent<ModPartComponent, BeingUnequippedAttemptEvent>(OnAttachedUnequipAttempt);

        SubscribeLocalEvent<ModSuitComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedVerbs);
        SubscribeLocalEvent<ModSuitComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ModPartComponent, GetVerbsEvent<EquipmentVerb>>(OnGetAttachedStripVerbsEvent);
        SubscribeLocalEvent<ModSuitComponent, ToggleModSuitDoAfterEvent>(OnDoAfterComplete);
    }

    private void GetRelayedVerbs(Entity<ModSuitComponent> modsuit, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbs(modsuit, ref args.Args);
    }

    private void OnGetVerbs(Entity<ModSuitComponent> modsuit, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var comp = modsuit.Comp;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || comp.ClothingUids.Count == 0 || comp.Container == null)
            return;

        var text = comp.VerbText ?? (comp.ActionEntity == null ? null : Name(comp.ActionEntity.Value));
        if (text == null)
            return;

        if (!_inventorySystem.InSlotWithFlags(modsuit.Owner, comp.RequiredFlags))
            return;

        var wearer = Transform(modsuit).ParentUid;
        if (args.User != wearer && comp.StripDelay == null)
            return;

        var user = args.User;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString(text),
        };

        if (user == wearer)
        {
            verb.EventTarget = modsuit;
            verb.ExecutionEventArgs = new ToggleClothingEvent() { Performer = args.User };
        }
        else
        {
            verb.Act = () => StartDoAfter(user, modsuit, wearer);
        }

        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid user, Entity<ModSuitComponent> modsuit, EntityUid wearer)
    {
        var comp = modsuit.Comp;

        if (comp.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, modsuit, comp.StripDelay.Value);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleModSuitDoAfterEvent(), modsuit, wearer, modsuit)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", modsuit));
            _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void OnGetAttachedStripVerbsEvent(Entity<ModPartComponent> attached, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var comp = attached.Comp;

        if (!TryComp<ModSuitComponent>(comp.AttachedUid, out var modsuitComp))
            return;

        // redirect to the attached entity.
        OnGetVerbs((comp.AttachedUid, modsuitComp), ref args);
    }

    private void OnDoAfterComplete(Entity<ModSuitComponent> modsuit, ref ToggleModSuitDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        TogglePart(args.User, modsuit);
    }

    private void OnInteractHand(Entity<ModPartComponent> attached, ref InteractHandEvent args)
    {
        var comp = attached.Comp;

        if (args.Handled)
            return;

        if (!TryComp(comp.AttachedUid, out ModSuitComponent? modsuitComp)
            || modsuitComp.Container == null)
            return;

        // Get slot from dictionary of uid-slot
        if (!modsuitComp.ClothingUids.TryGetValue(attached.Owner, out var attachedSlot))
            return;

        if (!_inventorySystem.TryUnequip(Transform(attached.Owner).ParentUid, attachedSlot, force: true))
            return;

        _containerSystem.Insert(attached.Owner, modsuitComp.Container);
        args.Handled = true;
    }

    /// <summary>
    /// Prevents from unequipping entity if all attached not unequipped
    /// </summary>
    private void OnModsuitUnequipAttempt(Entity<ModSuitComponent> modsuit, ref BeingUnequippedAttemptEvent args)
    {
        var comp = modsuit.Comp;

        if (!comp.BlockUnequipWhenAttached)
            return;

        if (CheckAttachedToggleStatus(modsuit) == ModSuitAttachedStatus.NoneToggled)
            return;

        args.Cancel();
        _popupSystem.PopupClient(Loc.GetString("modsuit-clothing-remove-all-attached-first"), args.Unequipee, args.Unequipee);
    }

    /// <summary>
    ///     Called when the suit is unequipped, to ensure that the helmet also gets unequipped.
    /// </summary>
    private void OnModsuitUnequip(Entity<ModSuitComponent> modsuit, ref GotUnequippedEvent args)
    {
        var comp = modsuit.Comp;

        // If it's a part of PVS departure then don't handle it.
        if (_timing.ApplyingState)
            return;

        // Check if container exists and we have linked clothings
        if (comp.Container == null || comp.ClothingUids.Count == 0)
            return;

        var parts = comp.ClothingUids;

        foreach (var part in parts)
        {
            // Check if entity in container what means it already unequipped
            if (comp.Container.Contains(part.Key))
                continue;

            if (part.Value == null)
                continue;

            _inventorySystem.TryUnequip(args.Equipee, part.Value, force: true);
        }
    }

    private void OnRemoveModsuit(Entity<ModSuitComponent> modsuit, ref ComponentRemove args)
    {
        // If the parent/owner component of the attached clothing is being removed (entity getting deleted?) we will
        // delete the attached entity. We do this regardless of whether or not the attached entity is currently
        // "outside" of the container or not. This means that if a hardsuit takes too much damage, the helmet will also
        // automatically be deleted.

        var comp = modsuit.Comp;

        _actionsSystem.RemoveAction(comp.ActionEntity);

        if (comp.ClothingUids == null || _netMan.IsClient)
            return;

        foreach (var clothing in comp.ClothingUids.Keys)
        {
            QueueDel(clothing);
        }
    }

    private void OnAttachedUnequipAttempt(Entity<ModPartComponent> attached, ref BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(Entity<ModPartComponent> attached, ref ComponentRemove args)
    {
        // if the attached component is being removed (maybe entity is being deleted?) we will just remove the
        // modsuit component. This means if you had a hard-suit helmet that took too much damage, you would
        // still be left with a suit that was simply missing a helmet. There is currently no way to fix a partially
        // broken suit like this.

        var comp = attached.Comp;

        if (!TryComp(comp.AttachedUid, out ModSuitComponent? modsuitComp))
            return;

        if (modsuitComp.LifeStage > ComponentLifeStage.Running)
            return;

        var clothingUids = modsuitComp.ClothingUids;

        if (!clothingUids.Remove(attached.Owner))
            return;

        // If no attached clothing left - remove component and action
        if (clothingUids.Count > 0)
            return;

        _actionsSystem.RemoveAction(modsuitComp.ActionEntity);
        RemComp(comp.AttachedUid, modsuitComp);
    }

    /// <summary>
    ///     Called if the clothing was unequipped, to ensure that it gets moved into the suit's container.
    /// </summary>
    private void OnAttachedUnequip(Entity<ModPartComponent> attached, ref GotUnequippedEvent args)
    {
        var comp = attached.Comp;

        // Let containers worry about it.
        if (_timing.ApplyingState)
            return;

        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!TryComp(comp.AttachedUid, out ModSuitComponent? modsuitComp))
            return;

        if (modsuitComp.LifeStage > ComponentLifeStage.Running)
            return;

        // As unequipped gets called in the middle of container removal, we cannot call a container-insert without causing issues.
        // So we delay it and process it during a system update:
        if (!modsuitComp.ClothingUids.ContainsKey(attached.Owner))
            return;

        if (modsuitComp.Container != null)
            _containerSystem.Insert(attached.Owner, modsuitComp.Container);
    }

    /// <summary>
    ///     Equip or unequip toggle clothing with ui message
    /// </summary>
    private void OnToggleClothingMessage(Entity<ModSuitComponent> modsuit, ref ModSuitUiMessage args)
    {
        var attachedUid = GetEntity(args.AttachedClothingUid);

        TogglePart(args.Actor, modsuit, attachedUid);
    }

    /// <summary>
    ///     Equip or unequip the MOD part.
    /// </summary>
    private void OnToggleMODPartAction(Entity<ModSuitComponent> modsuit, ref ToggleClothingEvent args)
    {
        var comp = modsuit.Comp;

        if (args.Handled)
            return;

        if (comp.Container == null || comp.ClothingUids.Count == 0)
            return;

        args.Handled = true;

        // If clothing have only one attached clothing (like helmets) action will just toggle it
        // If it have more attached clothings, it'll open radial menu
        if (comp.ClothingUids.Count == 1)
            TogglePart(args.Performer, modsuit, comp.ClothingUids.First().Key);
        else
            _uiSystem.OpenUi(modsuit.Owner, ToggleModSuitUiKey.Key, args.Performer);
    }

    private void TogglePart(EntityUid user, Entity<ModSuitComponent> modsuit, EntityUid? attachedUid = null)
    {
        var comp = modsuit.Comp;
        var attachedClothings = comp.ClothingUids;
        var container = comp.Container;

        if (!TryComp<WiresPanelComponent>(modsuit, out var panel) || panel.Open)
            return;
        if (container == null || attachedClothings.Count == 0)
            return;
        if (TryComp<ClothingComponent>(modsuit, out var clothingslot) && clothingslot.InSlot == null)
            return;
        // If container have more than one clothing and function wasn't invoked with UI message it should return null to prevent more problems
        if (attachedUid == null && attachedClothings.Count != 1)
            return;

        var parent = Transform(modsuit.Owner).ParentUid;

        if (attachedUid == null)
            attachedUid = attachedClothings.First().Key;

        if (!attachedClothings.TryGetValue(attachedUid.Value, out var slot))
            return;

        if (string.IsNullOrWhiteSpace(slot))
            return;

        TryComp<ModPartComponent>(attachedUid, out var attachedComp);

        // If not in clothing container - unequip the attached clothing
        if (!container.Contains(attachedUid.Value))
        {
            _inventorySystem.TryUnequip(user, parent, slot!, force: true);

            // If attached have clothing in container - equip it
            if (attachedComp == null || attachedComp.ClothingContainer == null)
                return;

            var storedClothing = attachedComp.ClothingContainer.ContainedEntity;

            if (storedClothing != null)
                _inventorySystem.TryEquip(user, storedClothing.Value, slot, force: true);

            return;
        }

        if (_inventorySystem.TryGetSlotEntity(parent, slot, out var currentClothing))
        {
            // Check if we need to replace current clothing
            if (attachedComp == null || !comp.ReplaceCurrentClothing)
            {
                _popupSystem.PopupClient(Loc.GetString("modsuit-clothing-remove-first", ("entity", currentClothing)), user, user);
                goto Equip;
            }

            // Check if attached clothing have container or this container not empty
            if (attachedComp.ClothingContainer == null || attachedComp.ClothingContainer.ContainedEntity != null)
                goto Equip;

            if (_inventorySystem.TryUnequip(user, parent, slot!))
                _containerSystem.Insert(currentClothing.Value, attachedComp.ClothingContainer);
        }

    Equip:
        _inventorySystem.TryEquip(user, parent, attachedUid.Value, slot, force: true);
    }

    private void OnGetActions(Entity<ModSuitComponent> modsuit, ref GetItemActionsEvent args)
    {
        var comp = modsuit.Comp;

        if (comp.ClothingUids.Count == 0)
            return;
        if (comp.ActionEntity == null)
            return;

        args.AddAction(comp.ActionEntity.Value);
    }

    private void OnMODSuitInit(Entity<ModSuitComponent> modsuit, ref ComponentInit args)
    {
        var comp = modsuit.Comp;

        comp.Container = _containerSystem.EnsureContainer<Container>(modsuit, comp.ContainerId);
    }

    private void OnAttachedInit(Entity<ModPartComponent> attached, ref ComponentInit args)
    {
        var comp = attached.Comp;

        comp.ClothingContainer = _containerSystem.EnsureContainer<ContainerSlot>(attached, comp.ClothingContainerId);
    }

    /// <summary>
    ///     On map init, either spawn the appropriate entity into the suit slot, or if it already exists, perform some
    ///     sanity checks. Also updates the action icon to show the MOD parts.
    /// </summary>
    private void OnMapInit(Entity<ModSuitComponent> modsuit, ref MapInitEvent args)
    {
        var comp = modsuit.Comp;

        if (comp.Container!.Count != 0)
        {
            DebugTools.Assert(comp.ClothingUids.Count != 0, "Unexpected entity present inside of a modsuit clothing container.");
            return;
        }

        if (comp.ClothingUids.Count != 0 && comp.ActionEntity != null)
            return;

        var xform = Transform(modsuit.Owner);

        if (comp.ClothingPrototypes == null)
            return;

        var prototypes = comp.ClothingPrototypes;

        foreach (var prototype in prototypes)
        {
            var spawned = Spawn(prototype.Value, xform.Coordinates);
            var attachedClothing = EnsureComp<ModPartComponent>(spawned);
            attachedClothing.AttachedUid = modsuit;
            EnsureComp<ContainerManagerComponent>(spawned);

            comp.ClothingUids.Add(spawned, prototype.Key);
            _containerSystem.Insert(spawned, comp.Container, containerXform: xform);

            Dirty(spawned, attachedClothing);
        }

        Dirty(modsuit, comp);

        if (_actionContainer.EnsureAction(modsuit, ref comp.ActionEntity, out var action, comp.Action))
            _actionsSystem.SetEntityIcon(comp.ActionEntity.Value, modsuit, action);
    }

    // Checks status of all attached clothings toggle status
    public ModSuitAttachedStatus CheckAttachedToggleStatus(Entity<ModSuitComponent> modsuit)
    {
        var comp = modsuit.Comp;
        var container = comp.Container;
        var attachedClothings = comp.ClothingUids;

        // If entity don't have any attached clothings it means none toggled
        if (container == null || attachedClothings.Count == 0)
            return ModSuitAttachedStatus.NoneToggled;

        var toggledCount = 0;

        foreach (var attached in attachedClothings)
        {
            if (container.Contains(attached.Key))
                continue;

            toggledCount++;
        }

        if (toggledCount == 0)
            return ModSuitAttachedStatus.NoneToggled;

        if (toggledCount < attachedClothings.Count)
            return ModSuitAttachedStatus.PartlyToggled;

        return ModSuitAttachedStatus.AllToggled;
    }
}

public sealed partial class ToggleModSuitEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ToggleModSuitDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Status of modsuit parts attachee
/// </summary>
[Serializable, NetSerializable]
public enum ModSuitAttachedStatus : byte
{
    NoneToggled,
    PartlyToggled,
    AllToggled
}
