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
    private ADTNpcCombatMemorySystem _memory = default!;

    private List<Vector2> _offsets = new();

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    [DataField]
    public int Radius = 4;

    [DataField]
    public int Attempts = 6;

    [DataField]
    public float SearchCooldown = 3f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _memory = sysManager.GetEntitySystem<ADTNpcCombatMemorySystem>();

        var offsets = new List<Vector2>();

        for (var x = -Radius; x <= Radius; x++)
        {
            for (var y = -Radius; y <= Radius; y++)
            {
                var offset = new Vector2(x, y);

                if (offset == Vector2.Zero || offset.Length() > Radius)
                    continue;

                offsets.Add(offset);
            }
        }

        offsets.Sort((a, b) => a.LengthSquared().CompareTo(b.LengthSquared()));
        _offsets = offsets;
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

        if (!_memory.TryBeginCoverSearch(owner, TimeSpan.FromSeconds(SearchCooldown)))
            return (false, null);

        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;
        var tries = 0;

        foreach (var offset in _offsets)
        {
            if (tries >= Attempts || cancelToken.IsCancellationRequested)
                break;

            var point = new MapCoordinates(ownerPos.Position + offset, ownerPos.MapId);

            if (!_interaction.InRangeUnobstructed(ownerPos, point, Radius + 1f, predicate: e => e == owner || e == target))
                continue;

            var exposure = Vector2.Distance(targetPos.Position, point.Position) + 1f;

            if (_interaction.InRangeUnobstructed(targetPos, point, exposure, predicate: e => e == target || e == owner))
                continue;

            tries++;

            var destinationCoords = ownerCoords.EntityId.IsValid()
                ? _transform.ToCoordinates(ownerCoords.EntityId, point)
                : _transform.ToCoordinates(point);

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
