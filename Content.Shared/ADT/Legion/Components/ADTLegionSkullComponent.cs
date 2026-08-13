using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Legion;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTLegionSkullComponent : Component
{
    [DataField]
    public EntProtoId LegionProto = "ADTMobLegion";

    [DataField]
    public bool CanInfestDead;

    [DataField]
    public int SwarmThreshold = 3;

    [DataField]
    public LocId InfestMessage = "adt-legion-infest";
}
