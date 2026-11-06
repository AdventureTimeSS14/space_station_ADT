using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTOpenStorageOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public string Key = ADTFindObstacleOperator.ObstacleKey;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var storage, _entManager) || _entManager.Deleted(storage))
            return HTNOperatorStatus.Failed;

        return _entManager.System<SharedEntityStorageSystem>().TryOpenStorage(owner, storage)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
