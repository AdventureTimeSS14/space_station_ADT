using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTIsGroupLeaderPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Range = 12f;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return (_entManager.System<ADTNpcCombatMemorySystem>().FindLeader(owner, Range) == owner) ^ Invert;
    }
}
