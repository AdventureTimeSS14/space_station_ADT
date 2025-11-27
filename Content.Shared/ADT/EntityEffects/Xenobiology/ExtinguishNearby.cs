using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

/// <summary>
///     Extinguishes nearby entities.
/// </summary>
public sealed partial class ExtinguishNearby : EventEntityEffect<ExtinguishNearby>
{

    [DataField]
    public float Range = 12;

    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-ignite", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;
}
