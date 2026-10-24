using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Implants.ImplantVision;

[Prototype]
public sealed partial class ImplantVisionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Shader = string.Empty;

    [DataField]
    public ImplantVisionTiming Timing = ImplantVisionTiming.None;

    [DataField]
    public Color? Tint;

    [DataField]
    public float TintAmount = 1f;

    [DataField]
    public bool DrawUserSprite;

    [DataField]
    public bool AboveEffects;
}

[Serializable, NetSerializable]
public enum ImplantVisionTiming : byte
{
    None,

    Elapsed,

    ElapsedAndTotal,
}
