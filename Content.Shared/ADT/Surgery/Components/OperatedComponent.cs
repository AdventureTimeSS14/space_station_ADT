using Content.Shared.ADT.Surgery.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OperatedComponent : Component
{
    [DataField]
    public ProtoId<SurgeryGraphPrototype>? GraphId;

    [DataField]
    public string CurrentNode = string.Empty;

    [DataField]
    public string? ActiveEdgeId;

    [DataField]
    public int CompletedSteps;

    [DataField]
    public EntityUid? Surgeon;

    [DataField]
    public bool IsOperating;
}
