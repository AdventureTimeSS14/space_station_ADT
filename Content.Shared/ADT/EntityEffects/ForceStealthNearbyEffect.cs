using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ForceStealthNearbyEffect : EntityEffectBase<ForceStealthNearbyEffect>
{
    [DataField]
    public float Radius = 7;

    [DataField]
    public float Duration = 30;

    [DataField]
    public float Chance = 1f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}