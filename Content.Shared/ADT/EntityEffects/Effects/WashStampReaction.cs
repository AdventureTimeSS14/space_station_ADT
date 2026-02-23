using Content.Shared.ADT.StampHit;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class WashStampReaction : EntityEffect
{
    protected override void Effect(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.HasComponent<StampedEntityComponent>(args.TargetEntity))
        {
            args.EntityManager.RemoveComponent<StampedEntityComponent>(args.TargetEntity);
        }
    }
}

public sealed partial class WashStamp : EntityEffectBase<WashStamp>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-wash-stamp-reaction", ("chance", Probability));
}
