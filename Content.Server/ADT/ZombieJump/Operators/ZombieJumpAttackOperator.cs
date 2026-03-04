using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.ZombieJump;
using Content.Server.ADT.ZombieJump.Preconditions;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.ADT.ZombieJump;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.ZombieJump.Operators;
public sealed partial class ZombieJumpAttackOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    [DataField("cooldown")]
    public float Cooldown = 10f;

    private ZombieJumpSystem _jumpSystem = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _jumpSystem = sysManager.GetEntitySystem<ZombieJumpSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return;

        if (!_entManager.EntityExists(target))
            return;

        if (!_entManager.TryGetComponent<ZombieJumpComponent>(owner, out var jumpComp))
            return;

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var ownerXform) ||
            !_entManager.TryGetComponent<TransformComponent>(target, out var targetXform) ||
            ownerXform.MapID != targetXform.MapID)
        {
            return;
        }

        _jumpSystem.ExecuteJump(owner, jumpComp);
        blackboard.SetValue("LastJumpTime", _gameTiming.CurTime.TotalSeconds);
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return (false, null);

        if (_entManager.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState > MobState.Critical)
            return (false, null);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var ownerXform = _entManager.GetComponent<TransformComponent>(owner);
        var targetXform = _entManager.GetComponent<TransformComponent>(target);

        if (ownerXform.MapID != targetXform.MapID)
            return (false, null);

        return (true, null);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        return HTNOperatorStatus.Finished;
    }
}
