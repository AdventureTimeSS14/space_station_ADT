using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Grab;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class ADTGrabEscapeOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    private ActionBlockerSystem _actionBlocker = default!;
    private PullingSystem _pulling = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _actionBlocker = sysManager.GetEntitySystem<ActionBlockerSystem>();
        _pulling = sysManager.GetEntitySystem<PullingSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<GrabbableComponent>(owner, out var grabbable)
            || !_entManager.TryGetComponent<PullableComponent>(owner, out var pullable))
            return;

        if (grabbable.GrabStage == GrabStage.No || _timing.CurTime < grabbable.NextEscapeAttempt)
            return;

        if (!_actionBlocker.CanInteract(owner, owner))
            return;

        _pulling.TryStopPull(owner, pullable, owner);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }
}
