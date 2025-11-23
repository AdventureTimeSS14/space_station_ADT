using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.ADT.Xenobiology.HTN;

public sealed partial class SlimeLatchOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private XenobiologySystem _slimeMobActions = default!;

    [DataField]
    public string LatchKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _slimeMobActions = sysManager.GetEntitySystem<XenobiologySystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(LatchKey);

        return _entManager.TryGetComponent<SlimeComponent>(owner, out var slime)
               && target.IsValid()
               && !_entManager.Deleted(target)
               && target != slime.LatchedTarget
               && _slimeMobActions.NpcTryLatch(owner, target, slime)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
