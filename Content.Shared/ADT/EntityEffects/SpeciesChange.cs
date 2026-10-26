using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class SpeciesChange : EntityEffectBase<SpeciesChange>
{
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> NewSpecies = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}