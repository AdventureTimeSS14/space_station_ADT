using Content.Shared.ADT.CriminalRecords;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Traits.Effects;

public sealed partial class ArrestWarrantEffect : BaseTraitEffect
{
    public override void Apply(TraitEffectContext ctx)
    {
        ctx.EntMan.EnsureComponent<ArrestWarrantComponent>(ctx.Player);
    }
}