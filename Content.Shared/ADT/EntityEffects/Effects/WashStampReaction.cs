using Content.Shared.ADT.StampHit;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class WashStampReaction : EntityEffectSystem<StampedEntityComponent, WashStamp>
{

    protected override void Effect(StampedEntityComponent> entity, ref EntityEffectEvent<WashStamp> args)
    {
        args.EntityManager.RemoveComponent<StampedEntityComponent>(args.TargetEntity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class WashStamp : EntityEffectBase<WashStamp>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-wash-stamp-reaction", ("chance", Probability));
}
