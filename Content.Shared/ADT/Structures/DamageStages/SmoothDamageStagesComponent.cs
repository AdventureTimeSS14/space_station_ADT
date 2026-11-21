using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Structures.DamageStages;

[RegisterComponent, NetworkedComponent]
public sealed partial class SmoothDamageStagesComponent : Component
{
    [DataField(required: true)]
    public List<FixedPoint2> Thresholds = new();
}

[Serializable, NetSerializable]
public enum SmoothDamageStageVisuals : byte
{
    Stage,
}
