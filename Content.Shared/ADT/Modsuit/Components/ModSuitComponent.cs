using Robust.Shared.Audio;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

/// <summary>
///     This component gives an item an action that will equip or un-equip some clothing e.g. hardsuits and hardsuit helmets.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitComponent : Component
{
    #region gui

    [DataField, AutoNetworkedField]
    public string BackgroundPath = "/Textures/ADT/Interface/Backgrounds/Modsuits/nanotrasen_background.png";
    [DataField, AutoNetworkedField]

    public Color BackpanelsColor = new Color(0.06f, 0.1f, 0.16f, 0.6f);

    #endregion gui
    /// <summary>
    ///     non-modifyed energy using. 1 toggled part - 1 energy per PowerCellDraw use
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxComplexity = 15;
    public const string DefaultClothingContainerId = "modsuit-part";
    public const string DefaultModuleContainerId = "mod-modules-container";

    /// <summary>
    ///     Action used to toggle the clothing on or off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ADTActionToggleMODPiece";
    [DataField, AutoNetworkedField]
    public EntProtoId MenuAction = "ADTActionToggleMODMenu";

    /// <summary>
    ///     non-modifyed energy using. 1 toggled part - 1 energy per PowerCellDraw use
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModEnergyBaseUsing = 0.5f;

    public float ModEnergyModifyedUsing = 1;

    /// <summary>
    ///     Default clothing entity prototype to spawn into the clothing container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? ClothingPrototype;

    /// <summary>
    ///     The inventory slot that the clothing is equipped to.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public string Slot = string.Empty;

    /// <summary>
    ///     Dictionary of inventory slots and entity prototypes to spawn into the clothing container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntProtoId> ClothingPrototypes = new();

    /// <summary>
    ///     Dictionary of clothing uids and slots
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, string> ClothingUids = new();

    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = DefaultClothingContainerId;
    [DataField, AutoNetworkedField]
    public string ModuleContainerId = DefaultModuleContainerId;

    [ViewVariables]
    public Container? Container;

    /// <summary>
    ///     Time it takes for this clothing to be toggled via the stripping menu verbs. Null prevents the verb from even showing up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? StripDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Text shown in the toggle-clothing verb. Defaults to using the name of the <see cref="ActionEntity"/> action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? VerbText;

    /// <summary>
    ///     If true it will block unequip of this entity until all attached clothing are removed
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockUnequipWhenAttached = true;

    /// <summary>
    ///     If true all attached will replace already equipped clothing on equip attempt
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReplaceCurrentClothing = true;
    /// <summary>
    /// Sound, playing when mod is fully enabled
    /// </summary>
    [DataField]
    public SoundSpecifier FullyEnabledSound = new SoundPathSpecifier("/Audio/ADT/Mecha/nominal.ogg");

    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredFlags = SlotFlags.BACK;
    public TimeSpan Toggletick;
    /// <summary>
    ///     Modules on start
    /// </summary>

    [DataField]
    public List<EntProtoId> StartingModules = [];

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ModuleContainer = default!;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
    [DataField, AutoNetworkedField]
    public EntityUid? ActionMenuEntity;
    [AutoNetworkedField]
    public int CurrentComplexity = 0;
    [AutoNetworkedField]
    public string? UserName = null;
    [AutoNetworkedField]
    public EntityUid? TempUser = null;
}
[Serializable, NetSerializable]
public enum ModSuitMenuUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum ModSuitUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ModSuitUiMessage : BoundUserInterfaceMessage
{
    public NetEntity AttachedClothingUid;

    public ModSuitUiMessage(NetEntity attachedClothingUid)
    {
        AttachedClothingUid = attachedClothingUid;
    }
}
[Serializable, NetSerializable]
public sealed class ModBoundUiState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, BoundUserInterfaceState?> EquipmentStates = new();
}
public sealed class ModModulesUiStateReadyEvent : EntityEventArgs
{
    public Dictionary<NetEntity, BoundUserInterfaceState?> States = new();  // ADT Mech UI Fix
}
[Serializable, NetSerializable]
public sealed class ModModuleRemoveMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public ModModuleRemoveMessage(NetEntity module)
    {
        Module = module;
    }
}
[Serializable, NetSerializable]
public sealed class ModModulActivateMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public ModModulActivateMessage(NetEntity module)
    {
        Module = module;
    }
}
[Serializable, NetSerializable]
public sealed class ModModulDeactivateMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public ModModulDeactivateMessage(NetEntity module)
    {
        Module = module;
    }
}
[Serializable, NetSerializable]
public sealed class ModLockMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public ModLockMessage(NetEntity module)
    {
        Module = module;
    }
}
