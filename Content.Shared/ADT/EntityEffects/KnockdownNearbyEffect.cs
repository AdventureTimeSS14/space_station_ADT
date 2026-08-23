using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;
public sealed partial class KnockdownNearbyEffect : EntityEffectBase<KnockdownNearbyEffect>
{
    [DataField]
    public float Radius = 4;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}