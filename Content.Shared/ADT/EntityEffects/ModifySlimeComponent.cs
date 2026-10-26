using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ModifySlimeComponent : EntityEffectBase<ModifySlimeComponent>
{
    [DataField]
    public int? ExtractBonus;

    [DataField]
    public int? MaxExtractBonus;

    [DataField]
    public int? OffspringBonus;

    [DataField]
    public int? MaxOffspringBonus;

    [DataField]
    public float? ChanceModifier;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}