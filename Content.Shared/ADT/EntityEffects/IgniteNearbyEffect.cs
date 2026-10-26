using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class IgniteNearbyEffect : EntityEffectBase<IgniteNearbyEffect>
{
    [DataField]
    public float Radius = 7;

    [DataField]
    public float FireStacks = 2;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-ignite-nearby");

    public override LogImpact? Impact => LogImpact.Medium;
}