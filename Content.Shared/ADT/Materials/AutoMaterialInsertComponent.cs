using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Components;

namespace Content.Shared.ADT.Materials;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoMaterialInsertComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<TagPrototype> Tag;
}

[ByRefEvent]
public readonly record struct AutoMaterialInsertedEvent(Dictionary<EntityUid, ItemStorageLocation> storedItems, EntityUid user, EntityUid receiver, EntityUid used)
{
    public readonly Dictionary<EntityUid, ItemStorageLocation> StoredItems = storedItems;
    public readonly EntityUid User = user;
    public readonly EntityUid Receiver = receiver;
    public readonly EntityUid Used = used;
}
