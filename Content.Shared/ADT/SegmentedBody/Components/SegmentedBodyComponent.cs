using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SegmentedBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SegmentedBodyComponent : Component
{
    /// <summary>
    /// How much segments will have body
    /// Количество сегментов тела
    /// </summary>
    [DataField]
    public int SegmentsCount = 1;

    /// <summary>
    /// Segment entity
    /// Прототип сегмента
    /// </summary>
    [DataField("midSegment", required: true)]
    public EntProtoId MiddleSegmentPrototype;

    /// <summary>
    /// The last segment will be replaced with this entity if not null
    /// Прототип последнего сегмента
    /// </summary>
    [DataField("endSegment")]
    public EntProtoId? EndSegmentPrototype;

    /// <summary>
    /// If segments will be sharing damage between body
    /// Будут ли сегменты передавать урон основному телу
    /// </summary>
    [DataField]
    public bool ShareDamage = true;

    /// <summary>
    /// Max distance between parts
    /// Максимальная дистанция между частями
    /// </summary>
    [DataField]
    public float JointsLength = 1f;

    /// <summary>
    /// List of all body segments
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public List<EntityUid> Segments = new();
}
