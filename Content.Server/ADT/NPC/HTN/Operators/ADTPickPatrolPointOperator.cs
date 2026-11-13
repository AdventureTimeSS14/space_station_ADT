using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTPickPatrolPointOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    private SharedTransformSystem _transform = default!;
    private PathfindingSystem _pathfinding = default!;

    [DataField]
    public float Radius = 5f;

    [DataField]
    public float MinDistance = 1.5f;

    [DataField]
    public int Attempts = 4;

    [DataField]
    public string HomeKey = "ADTPatrolHome";

    [DataField]
    public string TargetKey = "ADTPatrolCoordinates";

    [DataField]
    public string PathfindKey = "ADTPatrolPathfind";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var current = _transform.GetMoverCoordinates(owner);

        if (!blackboard.TryGetValue<EntityCoordinates>(HomeKey, out var home, _entManager) ||
            home.EntityId != current.EntityId)
        {
            home = current;
        }

        var returning = (current.Position - home.Position).Length() > Radius;
        var flags = _pathfinding.GetFlags(blackboard);
        var attempts = returning ? 1 : Attempts;

        for (var i = 0; i < attempts; i++)
        {
            var target = returning
                ? home
                : new EntityCoordinates(home.EntityId, home.Position + _random.NextVector2(MinDistance, Radius));

            var path = await _pathfinding.GetPath(owner, current, target, 0.5f, cancelToken, flags);

            if (path.Result != PathResult.Path)
                continue;

            return (true, new Dictionary<string, object>
            {
                { HomeKey, home },
                { TargetKey, target },
                { PathfindKey, path },
            });
        }

        return (false, null);
    }
}
