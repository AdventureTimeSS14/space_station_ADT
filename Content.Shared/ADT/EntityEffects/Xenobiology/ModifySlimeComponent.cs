using Content.Shared.ADT.Xenobiology.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

public sealed partial class ModifySlimeComponent : EntityEffect
{
    /// <summary>
    /// How many additional extracts will be produced?
    /// </summary>
    [DataField]
    public int? ExtractBonus;

    /// <summary>
    /// How many additional offspring MAY be produced?
    /// </summary>
    [DataField]
    public int? OffspringBonus;

    /// <summary>
    /// How much will we increase/decrease the mutation chance?
    /// </summary>
    [DataField]
    public float? ChanceModifier;

    /// <summary>
    /// Should we increase, or decrease the mutation chance?
    /// </summary>
    [DataField]
    public bool ShouldSubtract;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<SlimeComponent>(args.TargetEntity, out var slime))
            return;

        slime.ExtractsProduced += ExtractBonus ?? 0;
        slime.MaxOffspring += OffspringBonus ?? 0;

        if (ChanceModifier is { } chanceMod)
        {
            slime.MutationChance = Math.Clamp(
                slime.MutationChance + (ShouldSubtract ? -chanceMod : chanceMod),
                0f,
                1f
            );
        }
    }
}
