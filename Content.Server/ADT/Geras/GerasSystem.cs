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
using Content.Shared.Item;
using Content.Shared.Hands;
using Content.Shared.Anomaly.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Nuke;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;
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
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action to non geras
        if (!component.NoAction)
        {
            _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
        }
    }

    private bool HasForbiddenComponent(EntityUid uid)
    {
        return HasComp<NukeDiskComponent>(uid) ||
               HasComp<GhostRoleComponent>(uid) ||
               HasComp<MindContainerComponent>(uid) ||
               HasComp<MobStateComponent>(uid);
    }

    private void EjectForbiddenRecursive(EntityUid item, EntityUid owner)
    {
        if (!TryComp<StorageComponent>(item, out var storage))
            return;

        var containedList = storage.Container.ContainedEntities.ToArray();

        foreach (var contained in containedList)
        {
            if (HasForbiddenComponent(contained))
            {
                _container.Remove(contained, storage.Container, force: true);
                _transform.DropNextTo(contained, owner);
            }
            else
            {
                EjectForbiddenRecursive(contained, owner);
            }
        }
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return;

        if (HasComp<AnomalyComponent>(uid))
            return;

        if (!_actionBlocker.CanInteract(uid, null) || _mobState.IsDead(uid) || _mobState.IsIncapacitated(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("geras-popup-cant-use"), uid, uid);
            return;
        }

        if (TryComp<HandsComponent>(uid, out var handsComp))
        {
            foreach (var hand in handsComp.Hands.Values)
            {
                if (hand.HeldEntity != null)
                {
                    EjectForbiddenRecursive(hand.HeldEntity.Value, uid);
                    _handsSystem.TryDrop(uid, hand.HeldEntity.Value);
                }
            }
        }

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
        {
            _inventorySystem.TryUnequip(uid, "outerClothing", force: true);

            foreach (var slot in inventoryComp.Slots)
            {
                if (_inventorySystem.TryGetSlotEntity(uid, slot.Name, out var itemUid) && itemUid.HasValue)
                {
                    var item = itemUid.Value;
                    if (HasForbiddenComponent(item))
                    {
                        _inventorySystem.TryUnequip(uid, slot.Name, force: true);
                    }
                    else
                    {
                        EjectForbiddenRecursive(item, uid);
                    }
                }
            }
        }

        EjectForbiddenRecursive(uid, uid);

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

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", ent.Value)), ent.Value, Filter.PvsExcept(ent.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), ent.Value, ent.Value);

        args.Handled = true;
    }
}
