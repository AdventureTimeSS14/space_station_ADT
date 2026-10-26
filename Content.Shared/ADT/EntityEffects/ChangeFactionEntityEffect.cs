using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class ChangeFactionEntityEffect : EntityEffectBase<ChangeFactionEntityEffect>
{
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> NewFaction = default!;

    [DataField]
    public float Duration = 0f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}