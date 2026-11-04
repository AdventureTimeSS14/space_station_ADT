using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Body.Bones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ADTFracturedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1f;

    [DataField, AutoNetworkedField]
    public float DoAfterModifier = 1f;

    [DataField, AutoNetworkedField]
    public float SpreadLeft;

    [DataField, AutoNetworkedField]
    public float SpreadRight;

    [DataField]
    public float DropLeft;

    [DataField]
    public float DropRight;

    [DataField]
    public float FallChance;

    [DataField]
    public float LockedDamage;

    [DataField]
    public bool HasSplints;

    [DataField]
    public bool HasFractures;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan IgnoreUntil;

    [ViewVariables]
    public float Distance;
}
