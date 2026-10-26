using Content.Shared.EntityEffects;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.EntityEffects.Effects;

[ImplicitDataDefinitionForInheritors]
public sealed partial class ExplosionReactionEffect : EntityEffect
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ExplosionPrototype>))]
    public string ExplosionType = default!;

    [DataField]
    public float MaxIntensity = 5;

    [DataField]
    public float IntensitySlope = 1;

    [DataField]
    public float MaxTotalIntensity = 100;

    [DataField]
    public float IntensityPerUnit = 1;

    [DataField]
    public float TileBreakScale = 1f;

    [DataField]
    public float Delay = 0f;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new ExplosionEvent(ExplosionType, MaxIntensity, IntensitySlope, MaxTotalIntensity, IntensityPerUnit, TileBreakScale, Delay, target);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

public sealed partial class ExplosionEvent : EntityEffectBase<ExplosionEvent>
{
    public EntityUid Target;
    public string ExplosionType;
    public float MaxIntensity;
    public float IntensitySlope;
    public float MaxTotalIntensity;
    public float IntensityPerUnit;
    public float TileBreakScale;
    public float Delay;

    public ExplosionEvent(string explosionType, float maxIntensity, float intensitySlope, float maxTotalIntensity, float intensityPerUnit, float tileBreakScale, float delay, EntityUid target)
    {
        ExplosionType = explosionType;
        MaxIntensity = maxIntensity;
        IntensitySlope = intensitySlope;
        MaxTotalIntensity = maxTotalIntensity;
        IntensityPerUnit = intensityPerUnit;
        TileBreakScale = tileBreakScale;
        Delay = delay;
        Target = target;
    }
}