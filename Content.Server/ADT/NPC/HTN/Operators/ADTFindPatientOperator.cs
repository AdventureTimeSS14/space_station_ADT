using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.NPC.Helpers;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindPatientOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Range = 9f;

    [DataField]
    public float MinDamage = 0.1f;

    [DataField]
    public float MedipenCooldown = 30f;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var lookup = _entManager.System<EntityLookupSystem>();
        var transform = _entManager.System<SharedTransformSystem>();
        var faction = _entManager.System<NpcFactionSystem>();
        var mobState = _entManager.System<MobStateSystem>();
        var ignored = ADTNpcHealHelper.IgnoredHealItems(blackboard, _entManager);

        var ownerMap = transform.GetMapCoordinates(owner);
        var defib = ADTNpcMedicHelper.FindDefib(_entManager, owner);
        var hasMedipen = ADTNpcMedicHelper.FindMedipen(_entManager, owner) != null;

        EntityUid? best = null;
        string? bestAction = null;
        var bestScore = float.MinValue;

        foreach (var found in lookup.GetEntitiesInRange<NpcFactionMemberComponent>(ownerMap, Range))
        {
            var patient = found.Owner;

            if (patient == owner || !_entManager.HasComponent<MobStateComponent>(patient))
                continue;

            if (!faction.IsEntityFriendly(owner, patient))
                continue;

            var canHeal = ADTNpcHealHelper.FindHealItemFor(_entManager, owner, patient, ignored, out _) != null;
            string? action = null;
            var priority = 0f;

            if (mobState.IsDead(patient))
            {
                if (!ADTNpcMedicHelper.CanBeRevived(_entManager, patient))
                    continue;

                if (defib != null && ADTNpcMedicHelper.RevivableByZap(_entManager, patient, defib.Value))
                {
                    action = ADTNpcMedicHelper.ActionDefib;
                    priority = 30f;
                }
                else if (canHeal)
                {
                    action = ADTNpcMedicHelper.ActionHeal;
                    priority = 25f;
                }
            }
            else if (mobState.IsCritical(patient))
            {
                if (hasMedipen && !ADTNpcMedicHelper.MedipenRecentlyUsed(blackboard, _entManager, patient, _timing.CurTime, MedipenCooldown))
                {
                    action = ADTNpcMedicHelper.ActionMedipen;
                    priority = 40f;
                }
                else if (canHeal)
                {
                    action = ADTNpcMedicHelper.ActionHeal;
                    priority = 35f;
                }
            }
            else if (canHeal)
            {
                var bleeding = ADTNpcHealHelper.IsBleeding(_entManager, patient);

                if (bleeding || ADTNpcHealHelper.DamageFraction(_entManager, patient) >= MinDamage)
                {
                    action = ADTNpcMedicHelper.ActionHeal;
                    priority = bleeding ? 15f : 10f;
                }
            }

            if (action == null)
                continue;

            var score = priority - Vector2.Distance(ownerMap.Position, transform.GetMapCoordinates(patient).Position) * 0.1f;

            if (score <= bestScore)
                continue;

            best = patient;
            bestAction = action;
            bestScore = score;
        }

        if (best is not { } target || bestAction == null)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
            { ADTNpcMedicHelper.ActionKey, bestAction },
        }));
    }
}
