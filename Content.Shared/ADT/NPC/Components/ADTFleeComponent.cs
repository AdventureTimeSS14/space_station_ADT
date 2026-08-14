using Robust.Shared.GameStates;

namespace Content.Shared.ADT.NPC;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTFleeComponent : Component
{
    [DataField]
    public string TargetKey = "Target";

    [DataField]
    public string CoordinatesKey = "ADTFleeCoordinates";

    [DataField]
    public string RangeKey = "ADTFleeRange";

    [DataField]
    public float DefaultRange = 3f;

    [DataField]
    public float Distance = 6f;

    [DataField]
    public float UpdateThreshold = 2f;
}
