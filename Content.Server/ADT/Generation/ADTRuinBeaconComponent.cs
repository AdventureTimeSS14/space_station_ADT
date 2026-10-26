using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Generation;

[RegisterComponent]
public sealed partial class ADTRuinBeaconComponent : Component
{
    [DataField]
    public EntProtoId Beacon = "ADTGpsBeaconRuin";

    [DataField]
    public float Probability = 1f;
}
