using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Effects;

public sealed partial class ADTCureDiseases : EntityEffectBase<ADTCureDiseases>
{
    [DataField]
    public bool VirusesOnly;

    [DataField]
    public bool GiveImmunity = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-adt-cure-diseases",
            ("chance", Probability),
            ("viruses", VirusesOnly));
    }
}
