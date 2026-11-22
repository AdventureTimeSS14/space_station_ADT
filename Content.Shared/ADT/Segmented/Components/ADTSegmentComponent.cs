using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Segmented.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTSegmentComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Root;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Leader;

    [ViewVariables]
    public int Index;

    [DataField]
    public float DamageModifier = 1f;

    [DataField]
    public bool TakeDamage;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan StepStart;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan StepEnd;

    [ViewVariables, AutoNetworkedField]
    public float StepHeight;

    [ViewVariables]
    public float VisualLift;
}
