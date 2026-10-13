using System.Numerics;

namespace Content.Shared.ADT.Drake;

[RegisterComponent]
public sealed partial class ADTDrakeVisualEffectComponent : Component
{
    [DataField]
    public List<ADTDrakeVisualKeyframe> Keyframes = new();

    [DataField]
    public string LayerKey = "base";
}

[DataDefinition]
public sealed partial class ADTDrakeVisualKeyframe
{
    [DataField]
    public float Time;

    [DataField]
    public Vector2? Offset;

    [DataField]
    public float? Alpha;

    [DataField]
    public Vector2? Scale;

    [DataField]
    public string? State;

    [DataField]
    public ADTDrakeEasing Easing = ADTDrakeEasing.Linear;
}

public enum ADTDrakeEasing : byte
{
    Linear,
    OutBounce,
}
