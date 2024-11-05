using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Weather;

namespace Content.Server.ADT.Ghostbar;

/// <summary>
/// прототип самих гост баров
/// <see cref="GhostBarSystem"/>
/// </summary>
[Prototype("ghostbarMap")]
public sealed partial class GhostBarMapPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    /// путь до карты
    /// </summary>
    [DataField("path", required: true)]
    public string Path = string.Empty;

    /// <summary>
    /// включает/выключает пацифизм
    /// </summary>
    [DataField("pacified")]
    public bool Pacified = false;

    /// <summary>
    /// добавляет игрокам призрачную прозрачность, лучше не ставить меньше 0.8f
    /// </summary>
    [DataField("ghosted")]
    public float Ghosted = 1f;

    /// <summary>
    /// список профессий, которые могут быть в гостбаре
    /// </summary>
    [DataField]
    public List<JobPrototype> Jobs = new()
    {
        new JobPrototype { ID = "Passenger" },
        new JobPrototype { ID = "Bartender" },
        new JobPrototype { ID = "Botanist" },
        new JobPrototype { ID = "Chef" },
        new JobPrototype { ID = "Janitor" }
    };

    /// <summary>
    /// погода на карте. если не заполнять строку - её не будет.
    /// </summary>
    [DataField("weather")]
    public ProtoId<WeatherPrototype>? Weather = null;

    /// <summary>
    /// компоненты, добавляемые при заходе в гостбар человека(ТОЛЬКО КАСТОМНЫЕ, все компоненты туда лучше не добавлять)
    /// </summary>
    [DataField("componentsadd")]
    public ComponentRegistry Componentsadd = new();
}

