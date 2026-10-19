using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Hallucinations.EntityEffects;

public sealed partial class DissociativeExtendFront : EntityEffectBase<DissociativeExtendFront>
{
    [DataField]
    public float Seconds = 2f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-dissociative-extend", ("chance", Probability), ("seconds", Seconds));
    }
}

public sealed partial class DissociativeForceFront : EntityEffectBase<DissociativeForceFront>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-dissociative-force", ("chance", Probability));
    }
}
