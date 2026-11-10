using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Xenobiology.Potions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SlimeSpeedPotionAppliedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float OriginalWalkModifier = 1f;

    [DataField, AutoNetworkedField]
    public float OriginalSprintModifier = 1f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SlimeFireproofPotionAppliedComponent : Component
{
    [DataField]
    public bool HadTemperatureProtection;

    [DataField]
    public float OriginalHeatCoefficient = 1f;

    [DataField]
    public bool HadFireProtection;

    [DataField]
    public float OriginalFireReduction;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;
}
