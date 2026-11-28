using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class StasisHealActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompActionStasisHeal";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public float Speed = 1f;

    [DataField]
    public float BruteHeal = -0.1f;

    [DataField]
    public float OtherHeal = -0.05f;

    [DataField]
    public float BloodRestore = 0.25f;

    [DataField]
    public float BleedReduce = -0.01f;

    [ViewVariables]
    public bool Active;

    [ViewVariables]
    public float SavedWalkSpeed;

    [ViewVariables]
    public float SavedSprintSpeed;
}
