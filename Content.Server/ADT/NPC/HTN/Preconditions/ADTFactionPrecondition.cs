using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTFactionPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _entManager.System<NpcFactionSystem>().IsMember(owner, Faction.Id) ^ Invert;
    }
}
