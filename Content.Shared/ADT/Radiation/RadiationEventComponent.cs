using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Radiation;

[RegisterComponent, NetworkedComponent]
public sealed partial class RadiationEventComponent : Component
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Количество изначальной радиации
    /// </summary>
    [DataField("initialIntensity")]
    public float InitialIntensity = 30f;

    /// <summary>
    /// Время, после которого начать спад радиации.
    /// </summary>
    [DataField("decayDelay")]
    public float DecayDelay = 1f;

    /// <summary>
    /// Спад радиации за секунду
    /// </summary>
    [DataField("decayRate")]
    public float DecayRate = 0.5f;

    /// <summary>
    /// Интервал (в секундах) между уменьшением радиации.
    /// </summary>
    [DataField("decayRateInterval")]
    public float DecayRateInterval = 1f;
    
    public float TimeSinceStart = 0f;
    public float TimeSinceLastDecay = 0f;
    public bool Decaying = false;
}