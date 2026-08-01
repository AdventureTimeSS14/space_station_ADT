using Content.Shared.ADT.Lavaland.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Weather;

/// <summary>
/// Makes weather randomly happen every so often.
/// </summary>
[RegisterComponent]// ADT-Tweak
[AutoGenerateComponentPause]
public sealed partial class WeatherSchedulerComponent : Component
{
    /// <summary>
    /// Weather stages to schedule.
    /// </summary>
    [DataField(required: true)]
    public List<WeatherStage> Stages = new();

    /// <summary>
    /// The index of <see cref="Stages"/> to use next, wraps back to the start.
    /// </summary>
    [DataField]
    public int Stage;

    /// <summary>
    /// When to go to the next step of the schedule.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// ADT tweak: событие, которое надо запустить, когда закончится текущая стадия.
    /// </summary>
    [DataField]
    public ProtoId<ADTLavalandEventPrototype>? PendingEvent;
}

/// <summary>
/// A stage in a weather schedule.
/// </summary>
[Serializable, DataDefinition]
public partial struct WeatherStage
{
    /// <summary>
    /// A range of how long the stage can last for, in seconds.
    /// </summary>
    [DataField(required: true)]
    public MinMax Duration = new(0, 0);

    /// <summary>
    /// The weather status effect prototype to add, or null for clear weather.
    /// Ignored when <see cref="Variants"/> is filled in.
    /// </summary>
    [DataField]
    public EntProtoId? Weather;

    /// <summary>
    /// Alert message to send in chat for players on the map when it starts.
    /// Ignored when <see cref="Variants"/> is filled in.
    /// </summary>
    [DataField]
    public LocId? Message;

    /// <summary>
    /// ADT tweak: kinds of weather this stage picks between, weighted, the way SS13 rolls its storms.
    /// When this is set it replaces <see cref="Weather"/> and <see cref="Message"/>.
    /// </summary>
    [DataField]
    public List<WeatherStageVariant> Variants = new();

    [DataField]
    public ProtoId<ADTLavalandEventPrototype>? EventOnEnd; // ADT tweak
}

// ADT-Tweak-Start
[Serializable, DataDefinition]
public partial struct WeatherStageVariant
{
    /// <summary>
    /// The weather status effect prototype to add, or null for clear weather.
    /// </summary>
    [DataField]
    public EntProtoId? Weather;

    /// <summary>
    /// Alert message to send in chat for players on the map when it starts.
    /// </summary>
    [DataField]
    public LocId? Message;

    /// <summary>
    /// How likely this one is relative to the other variants of the stage.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    [DataField]
    public ProtoId<ADTLavalandEventPrototype>? EventOnEnd;
}
// ADT-Tweak-End