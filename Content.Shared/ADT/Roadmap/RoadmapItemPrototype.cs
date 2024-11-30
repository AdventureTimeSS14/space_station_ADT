using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Roadmap;

/// <summary>
/// A prototype for phantom styles.
/// </summary>
[Prototype("roadmap")]
public sealed partial class RoadmapItemPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name = "";

    [DataField("desc", required: true)]
    public string Description = "";

    [DataField("state", required: true)]
    public RoadmapItemState State = RoadmapItemState.Planned;
}

public enum RoadmapItemState
{
    Planned,
    InProgress,
    Partial,
    Complete
}
