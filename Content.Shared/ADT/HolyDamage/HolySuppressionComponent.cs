using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.HolyDamage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class HolySuppressionComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount;

    [DataField]
    public FixedPoint2 Max = 120;

    [DataField]
    public FixedPoint2 Recovery = 2;

    [DataField]
    public TimeSpan RecoveryInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRecovery;
}
