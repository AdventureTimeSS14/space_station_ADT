using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTTreatPatientOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;

    private const string ItemKey = "ADTTreatItem";
    private const string TimerKey = "ADTTreatTimer";
    private const string StartedKey = "ADTTreatStarted";
    private const string DefibKey = "ADTTreatDefib";
    private const string RecheckKey = "ADTTreatRecheck";
    private const string CombatKey = "ADTTreatCombat";

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 2f;

    [DataField]
    public float StartTimeout = 3f;

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
        blackboard.Remove<EntityUid>(DefibKey);
        blackboard.Remove<bool>(CombatKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        Release(blackboard);

        if (!blackboard.TryGetValue<bool>(CombatKey, out var combat, _entManager))
            return;

        blackboard.Remove<bool>(CombatKey);
        _entManager.System<SharedCombatModeSystem>().SetInCombatMode(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), combat);
    }

    private void Release(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (blackboard.TryGetValue<EntityUid>(ItemKey, out var item, _entManager) && !_entManager.Deleted(item))
        {
            if (_gear.IsMedipen(item) && _gear.SolutionVolume(item) <= 0)
                _entManager.System<SharedHandsSystem>().TryDrop(owner, item, checkActionBlocker: false, doDropInteraction: false);
            else
                _gear.Stow(owner, item);
        }

        blackboard.Remove<EntityUid>(ItemKey);

        if (blackboard.TryGetValue<EntityUid>(DefibKey, out var defib, _entManager) && !_entManager.Deleted(defib))
        {
            var toggle = _entManager.System<ItemToggleSystem>();

            if (toggle.IsActivated(defib))
                toggle.TryDeactivate(defib, owner);
        }

        blackboard.Remove<EntityUid>(DefibKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var patient, _entManager) || _entManager.Deleted(patient))
            return HTNOperatorStatus.Failed;

        if (!blackboard.TryGetValue<string>(ADTNpcGearSystem.MedActionKey, out var action, _entManager))
            return HTNOperatorStatus.Failed;

        if (!_entManager.System<SharedTransformSystem>().InRange(owner, patient, MaxDistance))
            return HTNOperatorStatus.Failed;

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        var started = blackboard.TryGetValue<bool>(StartedKey, out var flag, _entManager) && flag;
        var ignored = ADTNpcGearSystem.GetIgnored(blackboard, ADTNpcGearSystem.IgnoredHealItemsKey, _entManager);

        if (!started)
        {
            if (!blackboard.TryGetValue<bool>(CombatKey, out _, _entManager))
            {
                var combatMode = _entManager.System<SharedCombatModeSystem>();
                blackboard.SetValue(CombatKey, combatMode.IsInCombatMode(owner));
                combatMode.SetInCombatMode(owner, false);
            }

            var patientCoords = _entManager.GetComponent<TransformComponent>(patient).Coordinates;

            if (action == ADTNpcGearSystem.MedActionDefib)
            {
                if (_gear.FindDefib(owner) is not { } defib)
                    return HTNOperatorStatus.Failed;

                var toggle = _entManager.System<ItemToggleSystem>();

                if (!toggle.IsActivated(defib) && toggle.TryActivate(defib, owner))
                    blackboard.SetValue(DefibKey, defib);

                if (!_entManager.System<SharedDefibrillatorSystem>().TryStartZap(defib, patient, owner))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;
            }
            else
            {
                var medipen = action == ADTNpcGearSystem.MedActionMedipen;
                var item = medipen
                    ? _gear.FindMedipen(owner)
                    : _gear.FindHealItemFor(owner, patient, ignored, out _);

                if (item == null)
                    return HTNOperatorStatus.Failed;

                if (!_gear.TakeInHand(owner, item.Value, out _))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

                blackboard.SetValue(ItemKey, item.Value);

                var interaction = _entManager.System<SharedInteractionSystem>();

                if (!interaction.InRangeUnobstructed(owner, patient))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

                var volume = medipen ? _gear.SolutionVolume(item.Value) : default;

                interaction.UserInteraction(owner, patientCoords, patient);

                if (medipen)
                {
                    if (_gear.SolutionVolume(item.Value) >= volume)
                        return HTNOperatorStatus.Failed;

                    _gear.MarkMedipen(blackboard, patient);
                }
                else if (!_entManager.HasComponent<ActiveDoAfterComponent>(owner))
                {
                    return HTNOperatorStatus.Failed;
                }
            }

            blackboard.SetValue(StartedKey, true);
            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        if (action == ADTNpcGearSystem.MedActionHeal &&
            blackboard.TryGetValue<EntityUid>(ItemKey, out var current, _entManager) &&
            !_entManager.Deleted(current))
        {
            var recheck = (blackboard.TryGetValue<float>(RecheckKey, out var r, _entManager) ? r : 0f) + frameTime;

            if (recheck >= RecheckInterval)
            {
                recheck = 0f;

                var currentScore = _gear.HealScore(patient, current);
                _gear.FindHealItemFor(owner, patient, ignored, out var bestScore);

                if (currentScore <= 0 || currentScore < bestScore)
                {
                    Release(blackboard);
                    return HTNOperatorStatus.Finished;
                }
            }

            blackboard.SetValue(RecheckKey, recheck);
        }

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < DoAfterTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        Release(blackboard);
        return HTNOperatorStatus.Finished;
    }
}
