using System.Threading;
using System.Threading.Tasks;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Server.ADT.Xenobiology.Systems;

namespace Content.Server.ADT.Xenobiology.HTN;

public sealed partial class PickSlimeLatchTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _ent = default!;
    private NpcFactionSystem _factions = default!;
    private MobStateSystem _mobSystem = default!;
    private HungerSystem _hunger = default!;
    private PathfindingSystem _pathfinding = default!;
    private SlimeLatchSystem _latch = default!;

    [DataField(required: true)]
    public string RangeKey = string.Empty;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string LatchKey = string.Empty;

    [DataField]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _mobSystem = sysManager.GetEntitySystem<MobStateSystem>();
        _factions = sysManager.GetEntitySystem<NpcFactionSystem>();
        _hunger = sysManager.GetEntitySystem<HungerSystem>();
        _latch = sysManager.GetEntitySystem<SlimeLatchSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _ent)
            || !_ent.TryGetComponent<SlimeComponent>(owner, out var slimeComp)
            || !_ent.TryGetComponent<MobGrowthComponent>(owner, out var growthComp))
        {
            return (false, null);
        }

        var targets = new List<EntityUid>();

        // Проверяем, не прицеплен ли слайм уже
        if (_latch.IsLatched((owner, slimeComp)))
            return (false, null);

        // Проверяем голод для детёнышей
        if (growthComp.IsFirstStage && _hunger.IsHungerBelowState(owner, HungerThreshold.Peckish))
            return (false, null);

        foreach (var entity in _factions.GetNearbyHostiles(owner, range))
        {
            if (_ent.HasComponent<BeingLatchedComponent>(entity)
                || _ent.HasComponent<SlimeDamageOvertimeComponent>(entity)
                || _mobSystem.IsDead(entity))
                continue;

            // Детёныши не убивают своего хозяина
            if (growthComp.IsFirstStage && entity == slimeComp.Tamer)
                continue;

            // Не убивать хозяина, если голод не сильный
            if (entity == slimeComp.Tamer && _hunger.IsHungerBelowState(owner, HungerThreshold.Peckish))
                continue;

            targets.Add(entity);
        }

        foreach (var target in targets)
        {
            if (!_ent.TryGetComponent<TransformComponent>(target, out var xform))
                continue;

            var targetCoords = xform.Coordinates;
            var path = await _pathfinding.GetPath(owner, target, range, cancelToken);

            if (path.Result != PathResult.Path)
                continue;

            return (true, new Dictionary<string, object>()
            {
                { TargetKey, targetCoords },
                { LatchKey, target },
                { PathfindKey, path },
            });
        }

        return (false, null);
    }
}