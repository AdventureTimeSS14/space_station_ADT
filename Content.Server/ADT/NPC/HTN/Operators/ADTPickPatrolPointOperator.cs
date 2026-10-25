using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTPickPatrolPointOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    private SharedTransformSystem _transform = default!;

    [DataField]
    public float Radius = 5f;

    [DataField]
    public float MinDistance = 1.5f;

    [DataField]
    public string HomeKey = "ADTPatrolHome";

    [DataField]
    public string TargetKey = "ADTPatrolCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var current = _transform.GetMoverCoordinates(owner);

        if (!blackboard.TryGetValue<EntityCoordinates>(HomeKey, out var home, _entManager) ||
            home.EntityId != current.EntityId)
        {
            home = current;
        }

        EntityCoordinates target;

        if ((current.Position - home.Position).Length() > Radius)
            target = home;
        else
            target = new EntityCoordinates(home.EntityId, home.Position + _random.NextVector2(MinDistance, Radius));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { HomeKey, home },
            { TargetKey, target },
        }));
    }
}
