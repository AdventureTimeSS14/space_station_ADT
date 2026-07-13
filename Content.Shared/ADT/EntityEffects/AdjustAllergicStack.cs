using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustAllergicStack : EntityEffectBase<AdjustAllergicStack>
{
    /// <summary>
    /// Amount we're adjusting stack by.
    /// </summary>
    [DataField]
    public float Amount = 0f;

    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-adjust-allergic-stack",
            ("chance", Probability),
            ("amount", MathF.Abs(Amount).ToString("0.00")),
            ("positive", Amount <= 0));
    }
}
