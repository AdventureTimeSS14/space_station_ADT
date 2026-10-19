using Content.Server.ADT.NPC.Helpers;
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
        return (ADTNpcGroupHelper.FindLeader(_entManager, owner, Range) == owner) ^ Invert;
    }
}
