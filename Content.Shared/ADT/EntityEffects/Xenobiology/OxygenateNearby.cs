using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

/// <summary>
///     Saturates the lungs of nearby respirators.
/// </summary>
public sealed partial class OxygenateNearby : EventEntityEffect<OxygenateNearby>
{

    [DataField]
    public float Range = 7;

    [DataField]
    public float Factor = 10f;

    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-ignite", ("chance", Probability)); //In due time...

    public override LogImpact LogImpact => LogImpact.Medium;
}
