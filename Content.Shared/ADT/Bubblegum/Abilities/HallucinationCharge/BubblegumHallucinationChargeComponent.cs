using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumHallucinationChargeComponent : Component
{
    [DataField]
    public int HallucinationsNormal = 6;

    [DataField]
    public int HallucinationsSmash = 4;

    [DataField]
    public float Radius = 6f;

    [DataField]
    public EntProtoId HallucinationPrototype = "ADTMegaFaunaBubblegumHallucination";

    [DataField]
    public EntProtoId TelegraphPrototype = "ADTBubblegumChargeTelegraph";

    [DataField]
    public List<float> SmashWaveDelays = new List<float> { 1.4f, 1.2f, 1.1f };

    [DataField]
    public float NormalDelay = 1.2f;

    [DataField]
    public float ChargeSpeed = 12f;
}
