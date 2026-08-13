using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class MutateNearbyPlantsEntityEffect : EntityEffectBase<MutateNearbyPlantsEntityEffect>
{
    [DataField]
    public float Radius = 5;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}