using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTHealSelfOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;

    private const string ItemKey = "ADTHealItem";
    private const string TimerKey = "ADTHealTimer";
    private const string StartedKey = "ADTHealStarted";
    private const string RecheckKey = "ADTHealRecheck";

    [DataField]
    public float RoomTimeout = 3f;

    [DataField]
    public float DoAfterTimeout = 20f;

    [DataField]
    public float RecheckInterval = 1f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.SetValue(RecheckKey, 0f);
        blackboard.SetValue(StartedKey, false);
        blackboard.Remove<EntityUid>(ItemKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (blackboard.TryGetValue<EntityUid>(ItemKey, out var item, _entManager) && !_entManager.Deleted(item))
            _gear.Stow(owner, item);

        blackboard.Remove<EntityUid>(ItemKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var stored, _entManager) ? stored : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        var started = blackboard.TryGetValue<bool>(StartedKey, out var flag, _entManager) && flag;
        var ignored = ADTNpcGearSystem.GetIgnored(blackboard, ADTNpcGearSystem.IgnoredHealItemsKey, _entManager);

        if (!started)
        {
            if (_gear.FindHealItem(owner, ignored, out _) is not { } item)
                return HTNOperatorStatus.Failed;

            if (!_gear.TakeInHand(owner, item, out var hand))
            {
                if (hand == null)
                    return timer < RoomTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

                _gear.Ignore(blackboard, ADTNpcGearSystem.IgnoredHealItemsKey, item);
                return HTNOperatorStatus.Failed;
            }

            blackboard.SetValue(ItemKey, item);

            _entManager.System<SharedInteractionSystem>().UseInHandInteraction(owner, item);

            if (!_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            {
                _gear.Ignore(blackboard, ADTNpcGearSystem.IgnoredHealItemsKey, item);
                return HTNOperatorStatus.Failed;
            }

            blackboard.SetValue(StartedKey, true);
            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        if (!blackboard.TryGetValue<EntityUid>(ItemKey, out var current, _entManager) || _entManager.Deleted(current))
            return HTNOperatorStatus.Finished;

        var recheck = (blackboard.TryGetValue<float>(RecheckKey, out var r, _entManager) ? r : 0f) + frameTime;

        if (recheck >= RecheckInterval)
        {
            recheck = 0f;

            var currentScore = _gear.HealScore(owner, current);
            _gear.FindHealItem(owner, ignored, out var bestScore);

            if (currentScore <= 0 || currentScore < bestScore)
            {
                _gear.Stow(owner, current);
                blackboard.Remove<EntityUid>(ItemKey);
                return HTNOperatorStatus.Finished;
            }
        }

        blackboard.SetValue(RecheckKey, recheck);

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < DoAfterTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
