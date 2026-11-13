using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindPatientOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;
    private EntityLookupSystem _lookup = default!;
    private SharedTransformSystem _transform = default!;
    private NpcFactionSystem _faction = default!;
    private MobStateSystem _mobState = default!;

    private readonly HashSet<Entity<NpcFactionMemberComponent>> _found = new();

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

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _faction = sysManager.GetEntitySystem<NpcFactionSystem>();
        _mobState = sysManager.GetEntitySystem<MobStateSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var ignored = ADTNpcGearSystem.GetIgnored(blackboard, ADTNpcGearSystem.IgnoredHealItemsKey, _entManager);

        var ownerMap = _transform.GetMapCoordinates(owner);
        var defib = _gear.FindDefib(owner);
        var hasMedipen = _gear.FindMedipen(owner) != null;

        EntityUid? best = null;
        string? bestAction = null;
        var bestScore = float.MinValue;

        _found.Clear();
        _lookup.GetEntitiesInRange(ownerMap, Range, _found);

        foreach (var found in _found)
        {
            var patient = found.Owner;

            if (patient == owner || !_entManager.HasComponent<MobStateComponent>(patient))
                continue;

            if (!_faction.IsEntityFriendly(owner, patient))
                continue;

            string? action = null;
            var priority = 0f;

            if (_mobState.IsDead(patient))
            {
                if (!_gear.CanBeRevived(patient))
                    continue;

                if (defib != null && _gear.RevivableByZap(patient, defib.Value))
                {
                    action = ADTNpcGearSystem.MedActionDefib;
                    priority = 30f;
                }
                else if (_gear.FindHealItemFor(owner, patient, ignored, out _) != null)
                {
                    action = ADTNpcGearSystem.MedActionHeal;
                    priority = 25f;
                }
            }
            else if (_mobState.IsCritical(patient))
            {
                if (hasMedipen && !_gear.MedipenRecentlyUsed(blackboard, patient, MedipenCooldown))
                {
                    action = ADTNpcGearSystem.MedActionMedipen;
                    priority = 40f;
                }
                else if (_gear.FindHealItemFor(owner, patient, ignored, out _) != null)
                {
                    action = ADTNpcGearSystem.MedActionHeal;
                    priority = 35f;
                }
            }
            else
            {
                var bleeding = _gear.IsBleeding(patient);

                if ((bleeding || _gear.DamageFraction(patient) >= MinDamage) &&
                    _gear.FindHealItemFor(owner, patient, ignored, out _) != null)
                {
                    action = ADTNpcGearSystem.MedActionHeal;
                    priority = bleeding ? 15f : 10f;
                }
            }

            if (action == null)
                continue;

            var score = priority - Vector2.Distance(ownerMap.Position, _transform.GetMapCoordinates(patient).Position) * 0.1f;

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
            { ADTNpcGearSystem.MedActionKey, bestAction },
        }));
    }
}
