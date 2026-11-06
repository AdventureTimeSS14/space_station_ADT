using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Hallucinations.EntityEffects;

public sealed partial class ReduceHallucinations : EntityEffectBase<ReduceHallucinations>
{
    [DataField]
    public float Time = 2.0f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-reduce-hallucinations",
            ("chance", Probability),
            ("time", Time));
    }
}
