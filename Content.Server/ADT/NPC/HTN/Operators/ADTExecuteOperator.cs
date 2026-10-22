using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTExecuteOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string StartedKey = "ADTExecuteStarted";
    private const string TimerKey = "ADTExecuteTimer";
    private const string CombatKey = "ADTExecuteCombat";

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 1.6f;

    [DataField]
    public float GunExecutionTime = 4f;

    [DataField]
    public float Timeout = 12f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(StartedKey, false);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.Remove<bool>(CombatKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        if (!blackboard.TryGetValue<bool>(CombatKey, out var combat, _entManager))
            return;

        blackboard.Remove<bool>(CombatKey);
        _entManager.System<SharedCombatModeSystem>().SetInCombatMode(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), combat);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        if (blackboard.TryGetValue<bool>(StartedKey, out var started, _entManager) && started)
        {
            if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
                return timer < Timeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

            return HTNOperatorStatus.Finished;
        }

        if (!blackboard.TryGetValue<EntityUid>(Key, out var victim, _entManager) || _entManager.Deleted(victim))
            return HTNOperatorStatus.Failed;

        if (!_entManager.System<SharedTransformSystem>().InRange(owner, victim, MaxDistance))
            return HTNOperatorStatus.Failed;

        if (_entManager.System<SharedHandsSystem>().GetActiveItem(owner) is not { } weapon)
            return HTNOperatorStatus.Failed;

        if (!_entManager.System<SharedExecutionSystem>().CanBeExecuted(victim, owner))
            return HTNOperatorStatus.Failed;

        float duration;
        LocId message;

        if (_entManager.HasComponent<GunComponent>(weapon))
        {
            _entManager.System<ADTNpcGearSystem>().GetAmmo(weapon, out var count, out _);

            if (count <= 0)
                return HTNOperatorStatus.Failed;

            duration = GunExecutionTime;
            message = "execution-popup-gun-initial-external";
        }
        else if (_entManager.TryGetComponent(weapon, out ExecutionComponent? execution))
        {
            duration = execution.DoAfterDuration;
            message = execution.ExternalMeleeExecutionMessage;
        }
        else
        {
            return HTNOperatorStatus.Failed;
        }

        var combatMode = _entManager.System<SharedCombatModeSystem>();
        blackboard.SetValue(CombatKey, combatMode.IsInCombatMode(owner));
        combatMode.SetInCombatMode(owner, false);

        var args = new DoAfterArgs(_entManager, owner, TimeSpan.FromSeconds(duration), new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_entManager.System<SharedDoAfterSystem>().TryStartDoAfter(args))
            return HTNOperatorStatus.Failed;

        _entManager.System<SharedPopupSystem>().PopupEntity(
            Loc.GetString(message,
                ("attacker", Identity.Entity(owner, _entManager)),
                ("victim", Identity.Entity(victim, _entManager)),
                ("weapon", weapon)),
            owner,
            PopupType.MediumCaution);

        blackboard.SetValue(StartedKey, true);
        blackboard.SetValue(TimerKey, 0f);
        return HTNOperatorStatus.Continuing;
    }
}
