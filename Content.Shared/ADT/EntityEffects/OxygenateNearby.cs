using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class OxygenateNearby : EntityEffectBase<OxygenateNearby>
{
    [DataField]
    public float Range = 7;

    [DataField]
    public float Factor = 10f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}