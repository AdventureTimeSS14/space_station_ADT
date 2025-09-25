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

namespace Content.Shared.ADT.ModSuits;

public sealed partial class ModSuitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedIdCardSystem _id = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
