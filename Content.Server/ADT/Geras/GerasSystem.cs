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
    [Dependency] private readonly SharedHandsSystem _hands = default!;
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
        if (!TryComp<ContainerManagerComponent>(item, out var containerManager))
            return;

        foreach (var container in containerManager.Containers.Values)
        {
            var containedList = container.ContainedEntities.ToArray();
            foreach (var contained in containedList)
            {
                if (HasForbiddenComponent(contained))
                {
                    _container.Remove(contained, container, force: true);
                    _transform.DropNextTo(contained, owner);
                }
                else
                {
                    EjectForbiddenRecursive(contained, owner);
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

        if (TryComp<HandsComponent>(uid, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held, Transform(uid).Coordinates);
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
