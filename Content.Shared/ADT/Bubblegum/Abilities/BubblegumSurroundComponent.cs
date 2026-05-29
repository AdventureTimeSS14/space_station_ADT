using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumSurroundComponent : Component
{
    [DataField]
    public int Waves = 5;

    [DataField]
    public int HallucinationsPerWave = 2;

    [DataField]
    public float Radius = 4f;

    [DataField]
    public float WaveDelay = 0.6f;

    [DataField]
    public float SelfChargeDelay = 0.9f;

    [DataField]
    public EntProtoId HallucinationPrototype = "ADTMegaFaunaBubblegumHallucination";

    [DataField]
    public EntProtoId TelegraphPrototype = "ADTBubblegumChargeTelegraph";

    [DataField]
    public float ChargeSpeed = 12f;
}
