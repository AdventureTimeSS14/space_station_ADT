using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class InvisibilityActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompActionStealth";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public float PassiveVisibilityRate = -0.1f;

    [DataField]
    public float MovementVisibilityRate = 0.1f;

    [DataField]
    public float MinVisibility = -1f;

    [DataField]
    public float MaxVisibility = 1.5f;

    [ViewVariables]
    public bool Active;
}
