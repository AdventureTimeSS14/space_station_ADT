using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Training;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrainingProgressComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Strength;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool StaminaBonusApplied;
}
