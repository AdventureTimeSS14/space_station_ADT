using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Spike;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpikeComponent : Component
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
    public float EscapeChance = 0.3f;

    /// <summary>
    /// Шанс успешно помочь кому то на крюке слезть с него
    /// </summary>
    [DataField]
    public float PickupChance = 0.8f;

}
