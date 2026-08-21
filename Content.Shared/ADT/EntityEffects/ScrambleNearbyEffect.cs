using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ScrambleNearbyEffect : EntityEffectBase<ScrambleNearbyEffect>
{
    [DataField]
    public float Radius = 7;

    [DataField]
    public List<ProtoId<SpeciesPrototype>>? SpeciesWhitelist;

    [DataField]
    public List<ProtoId<SpeciesPrototype>>? SpeciesBlacklist;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}