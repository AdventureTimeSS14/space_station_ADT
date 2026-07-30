using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ScrambleNearbyEffect : EntityEffectBase<ScrambleNearbyEffect>
{
    [DataField]
    public float Radius = 7;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}