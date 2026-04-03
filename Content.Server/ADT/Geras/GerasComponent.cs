using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.ADT.Geras;

/// <summary>
/// This component assigns the entity with a polymorph action.
/// </summary>
[RegisterComponent]
public sealed partial class GerasComponent : Component
{
    [DataField] public ProtoId<PolymorphPrototype> GerasPolymorphId = "SlimeMorphGeras";

    [DataField] public EntProtoId GerasAction = "ADTActionMorphGeras";

    [DataField] public EntityUid? GerasActionEntity;

    [DataField] public bool NoAction = false;
}

[RegisterComponent]
public sealed partial class GerasForbiddenStorageComponent : Component
{
    [DataField]
    public EntityUid OriginalBody;

    [DataField]
    public Dictionary<EntityUid, RestoreInfo> StoredForbidden = new();
}

[DataDefinition, Serializable]
public sealed partial class RestoreInfo
{
    [DataField] public string? SlotName;
    [DataField] public EntityUid? ContainerEntity;
    [DataField] public string? ContainerId;
}
