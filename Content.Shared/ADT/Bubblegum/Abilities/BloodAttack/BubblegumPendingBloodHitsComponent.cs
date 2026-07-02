using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumPendingBloodHitsComponent : Component
{
    [DataField]
    public List<PendingBloodHit> Queue = new List<PendingBloodHit>();
}

[DataDefinition]
public sealed partial class PendingBloodHit
{
    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public EntityUid Target;

    [DataField]
    public MapCoordinates At;

    [DataField]
    public float Damage;

    [DataField]
    public bool IsGrab;
}
