using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Radiation;

[RegisterComponent, NetworkedComponent]
public sealed partial class RadiationEventComponent : Component
{
    /// <summary>
    /// Количество изначальной радиации
    /// </summary>
    [DataField("initialIntensity")]
    public float InitialIntensity;

    /// <summary>
    /// Время, после которого начать спад радиации.
    /// </summary>
    [DataField("decayDelay")]
    public float DecayDelay;

    /// <summary>
    /// Спад радиации за секунду
    /// </summary>
    [DataField("decayRate")]
    public float DecayRate;

    /// <summary>
    /// Интервал (в секундах) между уменьшением радиации.
    /// </summary>
    [DataField("decayRateInterval")]
    public float DecayRateInterval;
    
    public float TimeSinceStart = 0f;
    public float TimeSinceLastDecay = 0f;
    public bool Decaying = false;
}