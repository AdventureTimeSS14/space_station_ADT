using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumPendingWarpComponent : Component
{
    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public MapCoordinates Destination;
}
