using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTWaitOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string TimerKey = "ADTWaitTimer";

    [DataField]
    public float Seconds = 2f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, Seconds);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) - frameTime;
        blackboard.SetValue(TimerKey, timer);

        return timer <= 0f ? HTNOperatorStatus.Finished : HTNOperatorStatus.Continuing;
    }
}
