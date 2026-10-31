using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Hallucinations.EntityEffects;

public sealed partial class Hallucinate : EntityEffectBase<Hallucinate>
{
    [DataField("hallucinations", required: true)]
    public List<string> HallucinationPacks = default!;

    [DataField]
    public float Time = 2.0f;

    [DataField]
    public bool Refresh = true;

    [DataField]
    public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Update;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-hallucinations",
            ("chance", Probability),
            ("time", Time));
    }
}
