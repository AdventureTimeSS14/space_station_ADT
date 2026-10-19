using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTPickCoverOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private PathfindingSystem _pathfinding = default!;
    private SharedTransformSystem _transform = default!;
    private SharedInteractionSystem _interaction = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    [DataField]
    public int Radius = 4;

    [DataField]
    public int Attempts = 6;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return (false, null);

        var ownerPos = _transform.GetMapCoordinates(owner);
        var targetPos = _transform.GetMapCoordinates(target);

        if (ownerPos.MapId != targetPos.MapId)
            return (false, null);

        var candidates = new List<(Vector2 Pos, float Dist)>();

        for (var x = -Radius; x <= Radius; x++)
        {
            for (var y = -Radius; y <= Radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                var offset = new Vector2(x, y);
                var dist = offset.Length();

                if (dist > Radius)
                    continue;

                var point = new MapCoordinates(ownerPos.Position + offset, ownerPos.MapId);

                if (!_interaction.InRangeUnobstructed(ownerPos, point, Radius + 1f, predicate: e => e == owner || e == target))
                    continue;

                if (_interaction.InRangeUnobstructed(targetPos, point, 40f, predicate: e => e == target || e == owner))
                    continue;

                candidates.Add((point.Position, dist));
            }
        }

        candidates.Sort((a, b) => a.Dist.CompareTo(b.Dist));

        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;
        var tries = 0;

        foreach (var (pos, _) in candidates)
        {
            if (tries++ >= Attempts)
                break;

            var destination = new MapCoordinates(pos, ownerPos.MapId);
            var destinationCoords = ownerCoords.EntityId.IsValid()
                ? _transform.ToCoordinates(ownerCoords.EntityId, destination)
                : _transform.ToCoordinates(destination);

            var path = await _pathfinding.GetPath(
                owner,
                ownerCoords,
                destinationCoords,
                0.5f,
                cancelToken,
                flags: _pathfinding.GetFlags(blackboard));

            if (path.Result != PathResult.Path || path.Path.Count == 0)
                continue;

            return (true, new Dictionary<string, object>
            {
                { TargetCoordinates, destinationCoords },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }
}
