using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindObstacleOperator : HTNOperator
{
    private ADTNpcCombatMemorySystem _memory = default!;

    public const string ObstacleKey = "ADTObstacle";
    public const string ObstacleCoordinatesKey = "ADTObstacleCoordinates";

    [DataField]
    public float Range = 10f;

    [DataField]
    public float LineWidth = 0.7f;

    [DataField]
    public bool Openable;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _memory = sysManager.GetEntitySystem<ADTNpcCombatMemorySystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_memory.FindObstacle(owner, Range, LineWidth, Openable) is not { } obstacle)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { ObstacleKey, obstacle },
            { ObstacleCoordinatesKey, new EntityCoordinates(obstacle, Vector2.Zero) },
        }));
    }
}
