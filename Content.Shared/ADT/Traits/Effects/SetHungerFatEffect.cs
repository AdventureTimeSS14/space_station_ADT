using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.ADT.Traits.Effects;

public sealed partial class SetHungerFatEffect : BaseTraitEffect
{
    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent(ctx.Player, out HungerComponent? hunger))
            return;

        var max = hunger.Thresholds.TryGetValue(HungerThreshold.Fat, out var fat)
            ? fat
            : hunger.Thresholds[HungerThreshold.Overfed];
        ctx.EntMan.System<HungerSystem>().SetHunger(ctx.Player, max, hunger);
    }
}