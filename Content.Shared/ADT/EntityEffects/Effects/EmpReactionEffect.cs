using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects.Effects;

[ImplicitDataDefinitionForInheritors]
public sealed partial class EmpReactionEffect : EntityEffect
{
    [DataField]
    public float RangePerUnit = 0.5f;

    [DataField]
    public float MaxRange = 10;

    [DataField]
    public float EnergyConsumption = 12500;

    [DataField]
    public float Duration = 15;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new EmpEvent(RangePerUnit, MaxRange, EnergyConsumption, Duration, target);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

public sealed partial class EmpEvent : EntityEffectBase<EmpEvent>
{
    public EntityUid Target;
    public float RangePerUnit;
    public float MaxRange;
    public float EnergyConsumption;
    public float Duration;

    public EmpEvent(float rangePerUnit, float maxRange, float energyConsumption, float duration, EntityUid target)
    {
        RangePerUnit = rangePerUnit;
        MaxRange = maxRange;
        EnergyConsumption = energyConsumption;
        Duration = duration;
        Target = target;
    }
}