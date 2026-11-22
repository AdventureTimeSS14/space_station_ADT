using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Training;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TrainingProgressComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TrainedTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastTraining;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int Tier;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float StaminaBonus;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float UnarmedDamageBonus;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float PullDensityReduction;
}
