using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ExtinguishNearby : EntityEffectBase<ExtinguishNearby>
{
    [DataField]
    public float Range = 12;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-extinguish-nearby");

    public override LogImpact? Impact => LogImpact.Medium;
}