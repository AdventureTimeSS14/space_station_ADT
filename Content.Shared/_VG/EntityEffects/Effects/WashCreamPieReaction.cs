using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._VG.EntityEffects.Effects;

public sealed partial class WashCreamPieReaction : EntityEffectBase<WashCreamPieReaction>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Low;
}