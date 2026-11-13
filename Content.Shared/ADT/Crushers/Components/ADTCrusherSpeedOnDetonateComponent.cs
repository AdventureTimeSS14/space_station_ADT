using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTCrusherSpeedOnDetonateComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedPerStack = 0.05f;

    [DataField, AutoNetworkedField]
    public int MaxStacks = 4;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(4);

    [DataField]
    public EntProtoId SpeedEffect = "ADTStatusEffectCrusherMacheteSpeed";

    [ViewVariables]
    public int Stacks;

    [ViewVariables]
    public TimeSpan LastStack;
}
