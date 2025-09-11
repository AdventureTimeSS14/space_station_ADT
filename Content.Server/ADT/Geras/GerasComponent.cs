using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

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

