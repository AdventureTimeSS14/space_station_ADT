using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTStandUpOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string WaitKey = "ADTStandWait";
    private const string TotalKey = "ADTStandTotal";

    [DataField]
    public float RetryDelay = 4f;

    [DataField]
    public float Timeout = 20f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(WaitKey, 0f);
        blackboard.SetValue(TotalKey, 0f);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent(owner, out StandingStateComponent? standing) || standing.Standing)
            return HTNOperatorStatus.Finished;

        var total = (blackboard.TryGetValue<float>(TotalKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TotalKey, total);

        if (total > Timeout)
            return HTNOperatorStatus.Failed;

        var wait = blackboard.TryGetValue<float>(WaitKey, out var w, _entManager) ? w : 0f;

        if (wait > 0f)
        {
            blackboard.SetValue(WaitKey, wait - frameTime);
            return HTNOperatorStatus.Continuing;
        }

        if (_entManager.TryGetComponent(owner, out KnockedDownComponent? knocked))
        {
            if (_entManager.System<SharedStunSystem>().TryStanding((owner, knocked)))
                blackboard.SetValue(WaitKey, RetryDelay);
        }
        else
        {
            if (!_entManager.System<StandingStateSystem>().Stand(owner))
                blackboard.SetValue(WaitKey, 1f);
        }

        return HTNOperatorStatus.Continuing;
    }
}
