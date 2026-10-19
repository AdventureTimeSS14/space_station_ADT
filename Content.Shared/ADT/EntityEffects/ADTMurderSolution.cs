using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ADTCureCloneCellularDecay : EntityEffectBase<ADTCureCloneCellularDecay>
{
    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-cure-clone-cellular-decay",
            ("chance", Probability));
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ADTMurderSolutionBurst : EntityEffectBase<ADTMurderSolutionBurst>
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(3);

    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-murder-solution-burst",
            ("chance", Probability),
            ("amount", Damage.GetTotal().Float()),
            ("cooldown", Cooldown.TotalSeconds));
    }
}
