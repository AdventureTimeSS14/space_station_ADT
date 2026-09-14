using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumPendingChargeComponent : Component
{
    [DataField]
    public List<PendingCharge> Queue = [];
}

[DataDefinition]
public sealed partial class PendingCharge
{
    [DataField]
    public TimeSpan TelegraphAt;

    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public bool TelegraphSpawned;

    [DataField]
    public MapCoordinates Target;

    [DataField]
    public EntityUid? TargetEntity;

    [DataField]
    public float Speed = 8f;

    [DataField]
    public float TrampleDamage = 30f;

    [DataField]
    public bool ExpireOnHit;

    [DataField]
    public EntProtoId TelegraphProto = "ADTBubblegumChargeTelegraph";
}
