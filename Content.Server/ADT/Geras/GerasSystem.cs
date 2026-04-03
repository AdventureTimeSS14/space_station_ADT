using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.ADT.Geras;
using Robust.Shared.Player;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.ActionBlocker;
using Robust.Shared.Utility;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Inventory;
using Content.Shared.Nuke;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Clothing;
using Content.Shared.ADT.ModSuits;
using Content.Shared.ADT.Clothing.Components;
using Robust.Shared.Containers;
using Content.Shared.Polymorph;
using System.Linq;

namespace Content.Server.ADT.Geras;

public sealed class GerasSystem : SharedGerasSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ModSuitSystem _modSuitSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
        SubscribeLocalEvent<GerasForbiddenStorageComponent, PolymorphedEvent>(OnGerasReverted);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        if (!component.NoAction)
        {
            _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
        }
    }

    private bool HasForbiddenComponent(EntityUid uid)
    {
        return HasComp<NukeDiskComponent>(uid) ||
               HasComp<GhostRoleComponent>(uid) ||
               HasComp<MobStateComponent>(uid);
    }

    private Dictionary<EntityUid, RestoreInfo> CollectForbiddenFromEntity(EntityUid owner)
    {
        var collected = new Dictionary<EntityUid, RestoreInfo>();

        if (TryComp<InventoryComponent>(owner, out var inventoryComp))
        {
            foreach (var slot in inventoryComp.Slots)
            {
                if (_inventorySystem.TryGetSlotEntity(owner, slot.Name, out var itemUid) && itemUid.HasValue)
                {
                    var item = itemUid.Value;

                    if (HasForbiddenComponent(item))
                    {
                        collected[item] = new RestoreInfo { SlotName = slot.Name };
                        _inventorySystem.TryUnequip(owner, slot.Name, force: true);
                    }
                    else
                    {
                        CollectForbiddenRecursive(item, collected);
                    }
                }
            }
        }

        CollectForbiddenRecursive(owner, collected);

        return collected;
    }

    private void CollectForbiddenRecursive(EntityUid item, Dictionary<EntityUid, RestoreInfo> collected)
    {
        if (!TryComp<ContainerManagerComponent>(item, out var containerManager))
            return;

        foreach (var container in containerManager.Containers.Values)
        {
            var containedList = container.ContainedEntities.ToArray();
            foreach (var contained in containedList)
            {
                if (HasForbiddenComponent(contained))
                {
                    collected[contained] = new RestoreInfo
                    {
                        ContainerEntity = item,
                        ContainerId = container.ID
                    };

                    _container.Remove(contained, container, force: true);
                }
                else
                {
                    CollectForbiddenRecursive(contained, collected);
                }
            }
        }
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return;

        if (HasComp<AnomalyComponent>(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("geras-popup-cant-use-anomaly"), uid, uid);
            return;
        }

        if (!_actionBlocker.CanInteract(uid, null) || _mobState.IsDead(uid) || _mobState.IsIncapacitated(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("geras-popup-cant-use"), uid, uid);
            return;
        }

        if (_inventorySystem.TryGetSlotEntity(uid, "back", out var maybeModSuit) && maybeModSuit.HasValue)
        {
            if (TryComp<ModSuitComponent>(maybeModSuit.Value, out var modSuitComp))
            {
                _modSuitSystem.RemoveAllParts((maybeModSuit.Value, modSuitComp));
            }
        }

        if (_inventorySystem.TryGetSlotEntity(uid, "back", out var backItem) && backItem.HasValue)
        {
            var item = backItem.Value;
            if (HasComp<ClothingSpeedModifierComponent>(item) || HasComp<ModSuitComponent>(item) || HasComp<StorageOfHoldingComponent>(item))
            {
                _inventorySystem.TryUnequip(uid, "back", force: true);
            }
        }

        if (TryComp<HandsComponent>(uid, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held, Transform(uid).Coordinates);
            }
        }

        var forbiddenCollected = CollectForbiddenFromEntity(uid);

        var ent = _polymorphSystem.PolymorphEntity(uid, component.GerasPolymorphId);

        if (!ent.HasValue)
            return;

        var skinColor = Color.Green;

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanComp))
        {
            skinColor = humanComp.SkinColor;
        }

        if (TryComp<AppearanceComponent>(ent, out var appearanceComp))
        {
            _appearance.SetData(ent.Value, GeraColor.Color, skinColor, appearanceComp);
        }

        if (forbiddenCollected.Count > 0)
        {
            var storageComp = EnsureComp<GerasForbiddenStorageComponent>(ent.Value);
            storageComp.OriginalBody = uid;
            storageComp.StoredForbidden = forbiddenCollected;

            var forbiddenContainer = _container.EnsureContainer<Container>(ent.Value, "geras_forbidden_storage");

            foreach (var forbidden in forbiddenCollected.Keys)
            {
                if (Exists(forbidden))
                    _container.Insert(forbidden, forbiddenContainer, force: true);
            }
        }

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", ent.Value)), ent.Value, Filter.PvsExcept(ent.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), ent.Value, ent.Value);

        args.Handled = true;
    }

    private void OnGerasReverted(EntityUid uid, GerasForbiddenStorageComponent component, PolymorphedEvent args)
    {
        if (component.StoredForbidden.Count == 0 || !component.OriginalBody.IsValid() || !Exists(component.OriginalBody))
            return;

        var original = component.OriginalBody;

        foreach (var (forbidden, info) in component.StoredForbidden.ToArray())
        {
            if (!Exists(forbidden)) continue;

            if (_container.TryGetContainingContainer(forbidden, out var currentCont))
                _container.Remove(forbidden, currentCont, force: true);

            bool restored = false;

            if (info.SlotName != null)
            {
                restored = _inventorySystem.TryEquip(original, forbidden, info.SlotName, force: true);
            }

            if (!restored && info.ContainerEntity.HasValue && info.ContainerId != null)
            {
                if (TryComp<ContainerManagerComponent>(info.ContainerEntity.Value, out var contMan) &&
                    contMan.Containers.TryGetValue(info.ContainerId, out var targetContainer))
                {
                    restored = _container.Insert(forbidden, targetContainer, force: true);
                }
            }

            if (!restored)
                _transform.DropNextTo(forbidden, original);
        }

        component.StoredForbidden.Clear();
    }
}
