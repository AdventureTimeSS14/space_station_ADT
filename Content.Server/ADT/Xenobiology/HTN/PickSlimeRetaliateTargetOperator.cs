using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.Xenobiology;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology.HTN;

public sealed partial class PickSlimeRetaliateTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private PathfindingSystem _pathfinding = default!;
    private MobStateSystem _mobState = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinatesKey = "TargetCoordinates";

    [DataField]
    public string RangeKey = "MeleeRange";

    [DataField]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _mobState = sysManager.GetEntitySystem<MobStateSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_ent.TryGetComponent<SlimeRetaliateComponent>(owner, out var retaliate))
            return (false, null);

        if (_timing.CurTime >= retaliate.ExpiresAt
            || _ent.Deleted(retaliate.Attacker)
            || _mobState.IsDead(retaliate.Attacker)
            || !TryGetAttackerDistance(owner, retaliate.Attacker, out var distance)
            || distance > retaliate.MaxDistance)
        {
            _ent.RemoveComponentDeferred(owner, retaliate);
            return (false, null);
        }

        var attacker = retaliate.Attacker;

        if (!_ent.TryGetComponent<TransformComponent>(attacker, out var xform))
            return (false, null);

        var range = blackboard.GetValueOrDefault<float>(RangeKey, _ent);
        var path = await _pathfinding.GetPath(owner, attacker, range, cancelToken);

        if (path.Result != PathResult.Path)
            return (false, null);

        return (true, new Dictionary<string, object>()
        {
            { TargetKey, attacker },
            { TargetCoordinatesKey, xform.Coordinates },
            { PathfindKey, path },
        });
    }

    private bool TryGetAttackerDistance(EntityUid owner, EntityUid attacker, out float distance)
    {
        if (!_ent.TryGetComponent<TransformComponent>(owner, out var xform) ||
            !_ent.TryGetComponent<TransformComponent>(attacker, out var targetXform))
        {
            distance = 0f;
            return false;
        }

        return xform.Coordinates.TryDistance(_ent, targetXform.Coordinates, out distance);
    }
}