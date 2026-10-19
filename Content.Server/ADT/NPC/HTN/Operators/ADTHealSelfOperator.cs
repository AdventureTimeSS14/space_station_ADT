using Content.Server.ADT.NPC.Helpers;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTHealSelfOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string ItemKey = "ADTHealItem";
    private const string TimerKey = "ADTHealTimer";
    private const string StartedKey = "ADTHealStarted";

    [DataField]
    public float RoomTimeout = 3f;

    [DataField]
    public float DoAfterTimeout = 20f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.SetValue(StartedKey, false);
        blackboard.Remove<EntityUid>(ItemKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (blackboard.TryGetValue<EntityUid>(ItemKey, out var item, _entManager) && !_entManager.Deleted(item))
            ADTNpcItemHelper.Stow(_entManager, owner, item);

        blackboard.Remove<EntityUid>(ItemKey);
    }

    private void Ignore(NPCBlackboard blackboard, EntityUid item)
    {
        var ignored = ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager) ?? new HashSet<EntityUid>();
        ignored.Add(item);
        blackboard.SetValue(ADTNpcHealHelper.IgnoredKey, ignored);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var hands = _entManager.System<SharedHandsSystem>();

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var stored, _entManager) ? stored : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        var started = blackboard.TryGetValue<bool>(StartedKey, out var flag, _entManager) && flag;

        if (!started)
        {
            if (ADTNpcHealHelper.FindHealItem(_entManager, owner, ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager))
                is not { } item)
            {
                return HTNOperatorStatus.Failed;
            }

            if (!ADTNpcItemHelper.MakeRoom(_entManager, owner, out var hand) || hand == null)
                return timer < RoomTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

            if (!hands.TryPickup(owner, item, hand, checkActionBlocker: false, animate: false))
            {
                Ignore(blackboard, item);
                return HTNOperatorStatus.Failed;
            }

            hands.TrySetActiveHand(owner, hand);
            blackboard.SetValue(ItemKey, item);

            _entManager.System<SharedInteractionSystem>().UseInHandInteraction(owner, item);

            if (!_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            {
                Ignore(blackboard, item);
                return HTNOperatorStatus.Failed;
            }

            blackboard.SetValue(StartedKey, true);
            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        if (!blackboard.TryGetValue<EntityUid>(ItemKey, out var current, _entManager) || _entManager.Deleted(current))
            return HTNOperatorStatus.Finished;

        var currentScore = ADTNpcHealHelper.HealScore(_entManager, owner, current);
        ADTNpcHealHelper.FindHealItem(_entManager, owner, ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager), out var bestScore);

        if (currentScore <= 0 || currentScore < bestScore)
        {
            ADTNpcItemHelper.Stow(_entManager, owner, current);
            blackboard.Remove<EntityUid>(ItemKey);
            return HTNOperatorStatus.Finished;
        }

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < DoAfterTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
