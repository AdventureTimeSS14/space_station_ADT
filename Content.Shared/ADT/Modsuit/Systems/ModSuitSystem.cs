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
using Content.Shared.Mind;
using Content.Shared.Wires;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;
using Content.Shared.Coordinates;
using Content.Shared.PowerCell;
using Content.Shared.Access.Systems;
using Content.Shared.Emp;
using Robust.Shared.Player;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared.ADT.ModSuits;

public sealed partial class ModSuitSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedStrippableSystem _strippable = default!;
    [Dependency] private PowerCellSystem _cell = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedIdCardSystem _id = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ComponentTogglerSystem _componentToggler = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeSuit();
        InitializeParts();
        InitializeModules();
    }

    public void UpdateUserInterface(EntityUid uid, ModSuitComponent component)
    {
        _ui.SetUiState(uid, ModSuitUiKey.Key, new RadialModBoundUiState());

        Dirty(uid, component);

        var state = new ModBoundUiState();

        foreach (var ent in component.ModuleContainer.ContainedEntities)
        {
            if (!TryComp<ModSuitModComponent>(ent, out var mod))
                continue;

            state.EquipmentStates.Add(GetNetEntity(ent), mod.Active);
        }

        _ui.SetUiState(uid, ModSuitMenuUiKey.Key, state);
    }
}

/// <summary>
/// Status of modsuit attachee
/// </summary>
[Serializable, NetSerializable]
public enum ModSuitAttachedStatus : byte
{
    NoneToggled,
    PartlyToggled,
    AllToggled
}
