using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumPendingWavesComponent : Component
{
    [DataField]
    public List<PendingWave> Queue = [];
}

[DataDefinition]
public sealed partial class PendingWave
{
    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public MapCoordinates Target;

    [DataField]
    public EntityUid? TargetEntity;

    [DataField]
    public int Count;

    [DataField]
    public float Radius;

    [DataField]
    public float Delay;

    [DataField]
    public float Speed = 8f;

    [DataField]
    public EntProtoId HallucinationProto = "ADTMegaFaunaBubblegumHallucination";

    [DataField]
    public EntProtoId TelegraphProto = "ADTBubblegumChargeTelegraph";

    [DataField]
    public bool BossCharges;

    [DataField]
    public bool TripleChargeAfter;
}
