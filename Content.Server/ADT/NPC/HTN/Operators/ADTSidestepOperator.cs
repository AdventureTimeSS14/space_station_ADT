using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTSidestepOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private PathfindingSystem _pathfinding = default!;
    private SharedTransformSystem _transform = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
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

        var dir = targetPos.Position - ownerPos.Position;

        if (dir.LengthSquared() < 0.01f)
            return (false, null);

        dir = Vector2.Normalize(dir);
        var side = new Vector2(-dir.Y, dir.X);
        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;

        foreach (var offset in new[] { 1.5f, -1.5f, 2.5f, -2.5f })
        {
            var destination = new MapCoordinates(ownerPos.Position + side * offset, ownerPos.MapId);
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
