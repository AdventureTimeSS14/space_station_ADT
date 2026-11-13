using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTEatOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string ItemKey = "ADTEatItem";
    private const string HandKey = "ADTEatHand";
    private const string TimerKey = "ADTEatTimer";
    private const string TotalKey = "ADTEatTotal";
    private const string MaskKey = "ADTEatMaskDown";

    private ADTNpcGearSystem _gear = default!;

    [DataField]
    public float BiteInterval = 1.5f;

    [DataField]
    public float BiteTimeout = 10f;

    [DataField]
    public float TotalTimeout = 90f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.SetValue(TotalKey, 0f);
        blackboard.Remove<EntityUid>(ItemKey);
        blackboard.Remove<string>(HandKey);
        blackboard.SetValue(MaskKey, false);
    }

    private EntityUid? GetMask(EntityUid owner)
    {
        if (!_entManager.System<InventorySystem>().TryGetSlotEntity(owner, "mask", out var mask))
            return null;

        return _entManager.HasComponent<MaskComponent>(mask.Value) ? mask : null;
    }

    private void SetMask(EntityUid owner, EntityUid mask, bool down)
    {
        var args = new DoAfterArgs(_entManager, owner, TimeSpan.Zero, new ToggleMaskDoAfterEvent(down, false, false), mask, target: owner);
        _entManager.System<SharedDoAfterSystem>().TryStartDoAfter(args);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var hands = _entManager.System<SharedHandsSystem>();

        if (blackboard.TryGetValue<string>(HandKey, out var hand, _entManager) &&
            hands.TryGetHeldItem(owner, hand, out var held, true) &&
            _gear.Classify(held.Value) == null)
        {
            if (_entManager.HasComponent<EdibleComponent>(held.Value))
                _gear.Stow(owner, held.Value);
            else
                hands.TryDrop(owner, held.Value, checkActionBlocker: false, doDropInteraction: false);
        }

        blackboard.Remove<EntityUid>(ItemKey);
        blackboard.Remove<string>(HandKey);

        if (blackboard.TryGetValue<bool>(MaskKey, out var lowered, _entManager) && lowered && GetMask(owner) is { } worn)
            SetMask(owner, worn, false);

        blackboard.SetValue(MaskKey, false);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        if (GetMask(owner) is { } mask &&
            _entManager.TryGetComponent(mask, out MaskComponent? maskComp) &&
            !maskComp.IsToggled &&
            _entManager.HasComponent<IngestionBlockerComponent>(mask))
        {
            if (!maskComp.IsToggleable)
                return HTNOperatorStatus.Failed;

            if (!blackboard.TryGetValue<bool>(MaskKey, out var requested, _entManager) || !requested)
            {
                SetMask(owner, mask, true);
                blackboard.SetValue(MaskKey, true);
            }

            return timer < 3f ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;
        }

        if (!blackboard.TryGetValue<EntityUid>(ItemKey, out var food, _entManager))
        {
            if (_gear.FindFood(owner) is not { } found)
                return HTNOperatorStatus.Failed;

            if (!_gear.TakeInHand(owner, found, out var freeHand) || freeHand == null)
                return timer < 3f ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

            blackboard.SetValue(ItemKey, found);
            blackboard.SetValue(HandKey, freeHand);

            if (_entManager.TryGetComponent(found, out OpenableComponent? openable))
                _entManager.System<OpenableSystem>().TryOpen(found, openable, owner);

            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        var total = (blackboard.TryGetValue<float>(TotalKey, out var tt, _entManager) ? tt : 0f) + frameTime;
        blackboard.SetValue(TotalKey, total);

        if (total > TotalTimeout)
            return HTNOperatorStatus.Finished;

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < BiteTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        if (_entManager.Deleted(food) || !_entManager.HasComponent<EdibleComponent>(food))
            return HTNOperatorStatus.Finished;

        if (timer < BiteInterval)
            return HTNOperatorStatus.Continuing;

        blackboard.SetValue(TimerKey, 0f);
        _entManager.System<SharedInteractionSystem>().UseInHandInteraction(owner, food, checkUseDelay: false);

        return HTNOperatorStatus.Continuing;
    }
}
