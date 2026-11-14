using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Hallucinations.EntityEffects;

public sealed partial class Antipsychotic : EntityEffectBase<Antipsychotic>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-antipsychotic", ("chance", Probability));
    }
}
