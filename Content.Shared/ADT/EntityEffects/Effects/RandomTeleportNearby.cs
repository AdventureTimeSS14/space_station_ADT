using Content.Shared.EntityEffects;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.ADT.EntityEffects.Effects;

[ImplicitDataDefinitionForInheritors]
public sealed partial class RandomTeleportNearby : EntityEffect
{
    [DataField]
    public float Range = 7;

    [DataField]
    public MinMax Radius = new MinMax(5, 20);

    [DataField]
    public int TeleportAttempts = 10;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new RandomTeleportEvent(Range, Radius.Min, Radius.Max, TeleportAttempts, target);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

public sealed partial class RandomTeleportEvent : EntityEffectBase<RandomTeleportEvent>
{
    public EntityUid Target;
    public float Range;
    public float MinRadius;
    public float MaxRadius;
    public int TeleportAttempts;

    public RandomTeleportEvent(float range, float minRadius, float maxRadius, int teleportAttempts, EntityUid target)
    {
        Range = range;
        MinRadius = minRadius;
        MaxRadius = maxRadius;
        TeleportAttempts = teleportAttempts;
        Target = target;
    }
}