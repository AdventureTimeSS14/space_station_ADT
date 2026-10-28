using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery.Components;

[RegisterComponent]
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

    [ViewVariables]
    public bool IsOperating;

    [ViewVariables]
    public DoAfterId? DoAfter;
}
