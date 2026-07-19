using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumTripleChargeComponent : Component
{
    [DataField]
    public List<float> Delays = new() { 0.9f, 0.6f, 0.3f };

    [DataField]
    public EntProtoId TelegraphPrototype = "ADTBubblegumChargeTelegraph";

    [DataField]
    public float ChargeSpeed = 12f;

    [DataField]
    public EntProtoId MarkerPrototype = "ADTBubblegumTripleChargeMarker";

    [DataField]
    public TimeSpan PlayerCooldownBetweenClicks = TimeSpan.FromSeconds(0.3);

    [DataField]
    public TimeSpan FullCooldown = TimeSpan.FromSeconds(8);

    [DataField]
    public List<MapCoordinates> PendingPlayerTargets = [];

    [DataField]
    public List<EntityUid> PendingMarkers = [];
}
