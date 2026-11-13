using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTConstructionRestrictionComponent : Component
{
    [DataField]
    public HashSet<ProtoId<ConstructionPrototype>> AllowedRecipes = new();

    [ViewVariables]
    public HashSet<string>? AllowedGraphs;
}
