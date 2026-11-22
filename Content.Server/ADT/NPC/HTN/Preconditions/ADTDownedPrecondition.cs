using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Standing;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTDownedPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _entManager.TryGetComponent(owner, out StandingStateComponent? standing) && !standing.Standing;
    }
}
