using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SegmentedBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SegmentedBodyPartComponent : Component
{
    /// <summary>
    /// True if it is the last part
    /// True если это последняя часть
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Last = false;

    /// <summary>
    /// Should this part share damage with original body
    /// Будет ли часть передавать урон телу
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ShareDamage = true;

    /// <summary>
    /// True if part have been detached (just to not share damage from detached parts)
    /// True если часть была отсоединена
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Detached = false;

    /// <summary>
    /// Damage for this part to be detached
    /// Количество урона, необходимое для отделения части
    /// </summary>
    [AutoNetworkedField]
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 DetachDamage;

    /// <summary>
    /// Joints.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ParentJointId;

    /// <summary>
    /// Owner of <see cref="SegmentedBodyComponent"/>=
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? MainBody;
}
