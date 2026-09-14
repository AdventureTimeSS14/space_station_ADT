using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class SexChange : EntityEffectBase<SexChange>
{
    [DataField]
    public Sex? NewSex;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}