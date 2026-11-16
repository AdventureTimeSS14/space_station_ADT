using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ADTVampireGarlic : EntityEffectBase<ADTVampireGarlic>
{
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(20);

    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-vampire-garlic",
            ("chance", Probability));
    }
}