using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class Embalm : EntityEffectBase<Embalm>
{
    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-embalm",
            ("chance", Probability));
    }
}
