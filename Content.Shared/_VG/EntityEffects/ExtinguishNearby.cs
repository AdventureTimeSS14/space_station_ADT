using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._VG.EntityEffects;

public sealed partial class ExtinguishNearby : EntityEffectBase<ExtinguishNearby>
{
    [DataField]
    public float Range = 12;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}