using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

/// <summary>
///     Ignites mobs nearby.
/// </summary>
public sealed partial class IgniteNearby : EventEntityEffect<IgniteNearby>
{

    [DataField]
    public float Range = 7;

    [DataField]
    public float FireStacks = 2;

    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-ignite", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;
}
