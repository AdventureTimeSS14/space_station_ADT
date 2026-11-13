using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class AdjustOrganDamage : EntityEffectBase<AdjustOrganDamage>
{
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;

    [DataField(required: true)]
    public float Amount;

    [DataField]
    public OrganKind? Kind = OrganKind.Organic;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-adjust-organ-damage",
            ("chance", Probability),
            ("amount", MathF.Abs(Amount).ToString("0.00")),
            ("positive", Amount <= 0),
            ("organ", Category?.Id ?? string.Empty));
    }
}
