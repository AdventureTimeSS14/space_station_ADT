using Content.Shared.ADT.Implants.ImplantVision;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Implants.ImplantBoost;

[DataDefinition]
public sealed partial class ImplantBoostSettings
{
    [DataField]
    public float MovementSpeedModifier = 1f;

    [DataField]
    public float AttackSpeedModifier = 1f;

    [DataField]
    public float DoAfterSpeedModifier = 1f;

    [DataField]
    public ProtoId<ImplantVisionPrototype>? Vision;

    [DataField]
    public ImplantBoostTrail? Trail;

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public bool RemoveOnMobStateChange = true;
}

[DataDefinition]
public sealed partial class ImplantBoostTrail
{
    [DataField]
    public float LerpTime = 0.05f;

    [DataField]
    public TimeSpan LerpDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float Lifetime = 0.5f;

    [DataField]
    public float Frequency = 0.06f;

    [DataField]
    public float AlphaLerpAmount = 0.3f;

    [DataField]
    public int MaxParticleAmount = 15;

    [DataField]
    public Color? Color;

    [DataField]
    public bool Rainbow;
}
