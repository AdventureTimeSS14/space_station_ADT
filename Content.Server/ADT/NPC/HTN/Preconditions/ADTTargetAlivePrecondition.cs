using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Mobs.Systems;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTTargetAlivePrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public string TargetKey = "Target";

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return false;

        return _entManager.System<MobStateSystem>().IsAlive(target);
    }
}
