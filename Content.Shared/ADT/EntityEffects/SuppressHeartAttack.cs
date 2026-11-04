using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class SuppressHeartAttack : EntityEffectBase<SuppressHeartAttack>
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3);

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-suppress-heart-attack", ("chance", Probability));
    }
}
