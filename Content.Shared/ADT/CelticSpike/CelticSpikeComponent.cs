using Robust.Shared.GameStates;

namespace Content.Shared.ADT.CelticSpike;

[RegisterComponent, NetworkedComponent]
public sealed partial class CelticSpikeComponent : Component
{
    /// <summary>
    /// The entity that is currently impaled on the spike
    /// </summary>
    [DataField("impaledEntity")]
    public EntityUid? ImpaledEntity;

    /// <summary>
    /// Chance to successfully escape from the spike (0-1)
    /// </summary>
    [DataField]
    public float EscapeChance = 0.8f;

}
