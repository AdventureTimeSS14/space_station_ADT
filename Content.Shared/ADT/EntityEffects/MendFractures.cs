using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class MendFractures : EntityEffectBase<MendFractures>
{
    [DataField]
    public float BoneChance = 0.5f;

    [DataField]
    public bool MendOpen;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-mend-fractures", ("chance", Probability), ("open", MendOpen));
    }
}
