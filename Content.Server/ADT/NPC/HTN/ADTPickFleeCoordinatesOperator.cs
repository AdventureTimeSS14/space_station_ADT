using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class ADTPickFleeCoordinatesOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private PathfindingSystem _pathfinding = default!;
    private SharedTransformSystem _transform = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    [DataField]
    public float FleeDistance = 8f;

    [DataField]
    public float MinFleeDistance = 2f;

    [DataField]
    public float Spread = 0.6f;

    [DataField]
    public int Attempts = 8;

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

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            _entManager.Deleted(target))
        {
            return (false, null);
        }

        var ownerPos = _transform.GetMapCoordinates(owner);
        var targetPos = _transform.GetMapCoordinates(target);

        if (ownerPos.MapId != targetPos.MapId)
            return (false, null);

        var away = ownerPos.Position - targetPos.Position;

        var baseAngle = away.LengthSquared() > 0.01f
            ? new Angle(away)
            : _random.NextAngle();

        var attempts = Math.Max(1, Attempts);
        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;

        for (var i = 0; i < attempts; i++)
        {
            var progress = attempts == 1 ? 0f : i / (float)(attempts - 1);
            var spread = Spread + (MathF.PI / 2f - Spread) * progress;
            var distance = FleeDistance + (MinFleeDistance - FleeDistance) * progress;

            var angle = baseAngle + _random.NextFloat(-spread, spread);
            var destination = new MapCoordinates(ownerPos.Position + angle.ToVec() * distance, ownerPos.MapId);

            var destinationCoords = ownerCoords.EntityId.IsValid()
                ? _transform.ToCoordinates(ownerCoords.EntityId, destination)
                : _transform.ToCoordinates(destination);

            var path = await _pathfinding.GetPath(
                owner,
                ownerCoords,
                destinationCoords,
                1f,
                cancelToken,
                flags: _pathfinding.GetFlags(blackboard));

            if (path.Result is not (PathResult.Path or PathResult.PartialPath) || path.Path.Count == 0)
                continue;

            return (true, new Dictionary<string, object>
            {
                { TargetCoordinates, path.Path[^1].Coordinates },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }
}
