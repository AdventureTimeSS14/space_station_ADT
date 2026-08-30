using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ChangeFactionNearbyEffect : EntityEffectBase<ChangeFactionNearbyEffect>
{
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> NewFaction = default!;

    [DataField]
    public float Duration = 0f;

    [DataField]
    public float Radius = 5f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-faction-nearby", ("duration", Duration));

    public override LogImpact? Impact => LogImpact.Medium;
}