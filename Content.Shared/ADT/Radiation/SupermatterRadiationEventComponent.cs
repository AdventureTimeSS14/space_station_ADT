using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Radiation;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterRadiationEventComponent : Component
{
    [DataField]
    public float InitialIntensity = 30f;

    /// <summary>
    /// Секунды до уменьшения рады
    /// </summary>
    [DataField]
    public float DecayDelay = 1f;

    /// <summary>
    /// Количество снимаемой рады за секунду
    /// </summary>
    [DataField]
    public float DecayRate = 0.5f;

    public float TimeSinceStart = 0f;
    public bool Decaying = false;
}