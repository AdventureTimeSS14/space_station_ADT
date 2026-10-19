using System.Linq;
using System.Numerics;
using Content.Server.ADT.NPC.Helpers;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTTreatPatientOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const string ItemKey = "ADTTreatItem";
    private const string TimerKey = "ADTTreatTimer";
    private const string StartedKey = "ADTTreatStarted";

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 2f;

    [DataField]
    public float StartTimeout = 3f;

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
        Release(blackboard);
    }

    private void Release(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (blackboard.TryGetValue<EntityUid>(ItemKey, out var item, _entManager) && !_entManager.Deleted(item))
        {
            var id = _entManager.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID;

            if (id != null && ADTNpcMedicHelper.Medipens.Contains(id))
                _entManager.System<SharedHandsSystem>().TryDrop(owner, item, checkActionBlocker: false, doDropInteraction: false);
            else
                ADTNpcItemHelper.Stow(_entManager, owner, item);
        }

        blackboard.Remove<EntityUid>(ItemKey);
    }

    private bool TakeInHand(EntityUid owner, EntityUid item)
    {
        var hands = _entManager.System<SharedHandsSystem>();

        if (!ADTNpcItemHelper.MakeRoom(_entManager, owner, out var hand) || hand == null)
            return false;

        if (!hands.TryPickup(owner, item, hand, checkActionBlocker: false, animate: false))
            return false;

        hands.TrySetActiveHand(owner, hand);
        return true;
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var patient, _entManager) || _entManager.Deleted(patient))
            return HTNOperatorStatus.Failed;

        if (!blackboard.TryGetValue<string>(ADTNpcMedicHelper.ActionKey, out var action, _entManager))
            return HTNOperatorStatus.Failed;

        var transform = _entManager.System<SharedTransformSystem>();

        if (Vector2.Distance(transform.GetMapCoordinates(owner).Position, transform.GetMapCoordinates(patient).Position) > MaxDistance)
            return HTNOperatorStatus.Failed;

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        var started = blackboard.TryGetValue<bool>(StartedKey, out var flag, _entManager) && flag;

        if (!started)
        {
            _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false);
            var patientCoords = _entManager.GetComponent<TransformComponent>(patient).Coordinates;

            if (action == ADTNpcMedicHelper.ActionDefib)
            {
                if (ADTNpcMedicHelper.FindDefib(_entManager, owner) is not { } defib)
                    return HTNOperatorStatus.Failed;

                var toggle = _entManager.System<ItemToggleSystem>();

                if (!toggle.IsActivated(defib))
                    toggle.TryActivate(defib, owner);

                if (!_entManager.System<SharedDefibrillatorSystem>().TryStartZap(defib, patient, owner))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;
            }
            else
            {
                EntityUid? item = action == ADTNpcMedicHelper.ActionMedipen
                    ? ADTNpcMedicHelper.FindMedipen(_entManager, owner)
                    : ADTNpcHealHelper.FindHealItemFor(_entManager, owner, patient,
                        ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager), out _);

                if (item == null)
                    return HTNOperatorStatus.Failed;

                if (!TakeInHand(owner, item.Value))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

                blackboard.SetValue(ItemKey, item.Value);
                _entManager.System<SharedInteractionSystem>().UserInteraction(owner, patientCoords, patient);

                if (action == ADTNpcMedicHelper.ActionMedipen)
                    ADTNpcMedicHelper.MarkMedipen(blackboard, _entManager, patient, _timing.CurTime);
            }

            blackboard.SetValue(StartedKey, true);
            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        if (action == ADTNpcMedicHelper.ActionHeal &&
            blackboard.TryGetValue<EntityUid>(ItemKey, out var current, _entManager) &&
            !_entManager.Deleted(current))
        {
            var currentScore = ADTNpcHealHelper.HealScore(_entManager, patient, current);
            ADTNpcHealHelper.FindHealItemFor(_entManager, owner, patient,
                ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager), out var bestScore);

            if (currentScore <= 0 || currentScore < bestScore)
            {
                Release(blackboard);
                return HTNOperatorStatus.Finished;
            }
        }

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < DoAfterTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        if (action == ADTNpcMedicHelper.ActionDefib && ADTNpcMedicHelper.FindDefib(_entManager, owner) is { } used)
            _entManager.System<ItemToggleSystem>().TryDeactivate(used, owner);

        Release(blackboard);
        return HTNOperatorStatus.Finished;
    }
}
