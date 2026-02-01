using Content.Shared.Hands.Components;
using Content.Shared.Whitelist;
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

    /// <summary>
    /// ADT: The entities from <see cref="Items"/> that were spawned.
    /// </summary>
    [DataField("droppableProvidedItems")]
    public SortedDictionary<string, (EntityUid, DroppableBorgItem)> DroppableProvidedItems = new();

    /// <summary>
    /// Whether or not the items have been created and stored in <see cref="ProvidedContainer"/>
    /// </summary>
    [DataField("itemsCrated")]
    public bool ItemsCreated;

    /// <summary>
    /// A container where provided items are stored when not being used.
    /// This is helpful as it means that items retain state.
    /// </summary>
    [ViewVariables]
    public Container ProvidedContainer = default!;

    /// <summary>
    /// An ID for the container where provided items are stored when not used.
    /// </summary>
    [DataField("providedContainerId")]
    public string ProvidedContainerId = "provided_container";

    /// <summary>
    /// A counter that ensures a unique
    /// </summary>
    [DataField("handCounter")]
    public int HandCounter;
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
