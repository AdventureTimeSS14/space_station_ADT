using Content.Shared.ADT.Economy;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Traits.Effects;

public sealed partial class StartingBalanceEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public int Balance;

    public override void Apply(TraitEffectContext ctx)
    {
        var comp = ctx.EntMan.EnsureComponent<StartingBalanceComponent>(ctx.Player);
        comp.Balance = Balance;
        ctx.EntMan.Dirty(ctx.Player, comp);
    }
}