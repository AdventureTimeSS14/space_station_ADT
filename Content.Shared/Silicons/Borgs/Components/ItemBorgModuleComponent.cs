using Content.Shared.Whitelist; // ADT tweak
﻿using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for a <see cref="BorgModuleComponent"/> that provides items to the entity it's installed into.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class ItemBorgModuleComponent : Component
{
    /// <summary>
    /// The hands that are provided.
    /// </summary>
    [DataField(required: true)]
    public List<BorgHand> Hands = new();

    /// <summary>
    /// ADT: The droppable items that are provided.
    /// </summary>
    [DataField]
    public List<DroppableBorgItem> DroppableItems = new();

    /// <summary>
    /// The entities from <see cref="Items"/> that were spawned.
    /// The items stored within the hands. Null until the first time items are stored.
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid>? StoredItems;

    /// <summary>
    /// An ID for the container where items are stored when not in use.
    /// </summary>
    [DataField]
    public string HoldingContainer = "holding_container";
}

[DataDefinition, Serializable, NetSerializable]
public partial record struct BorgHand
{
    [DataField]
    public EntProtoId? Item;

    [DataField]
    public Hand Hand = new();

    [DataField]
    public bool ForceRemovable = false;

    public BorgHand(EntProtoId? item, Hand hand, bool forceRemovable = false)
    {
        Item = item;
        Hand = hand;
        ForceRemovable = forceRemovable;
    }
}
// ADT: droppable borg item data definitions
[DataDefinition]
public sealed partial class DroppableBorgItem
{
    [IdDataField]
    public EntProtoId ID;

    [DataField]
    public EntityWhitelist Whitelist;
}
