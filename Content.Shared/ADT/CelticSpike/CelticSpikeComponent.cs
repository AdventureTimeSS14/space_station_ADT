using Robust.Shared.GameStates;

namespace Content.Shared.ADT.CelticSpike;

[RegisterComponent, NetworkedComponent]
public sealed partial class CelticSpikeComponent : Component
{
    /// <summary>
    /// Энтити который находится на крюке
    /// </summary>
    [DataField("impaledEntity")]
    public EntityUid? ImpaledEntity;

    /// <summary>
    /// Шанс успешно сбежать с крюка
    /// </summary>
    [DataField]
    public float EscapeChance = 0.8f;

    /// <summary>
    /// Шанс успешно помочь кому то на крюке слезть с него
    /// </summary>
    [DataField]
    public float PickupChance = 0.3f;

}
