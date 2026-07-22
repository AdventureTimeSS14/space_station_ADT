using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server.ADT.Bubblegum.HTN;

public sealed partial class BubblegumRetreatOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedTransformSystem _transform = default!;

    [DataField]
    public string TargetKey = "Target";

    [DataField]
    public string RetreatCoordinatesKey = "BubblegumRetreatCoordinates";

    [DataField]
    public float RetreatDistance = 5f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        await Task.CompletedTask;

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return (false, null);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var ownerXform)
            || ownerXform.GridUid == null)
            return (false, null);

        var ownerMap = _transform.GetMapCoordinates(owner);
        var targetMap = _transform.GetMapCoordinates(target);
        if (ownerMap.MapId != targetMap.MapId)
            return (false, null);

        var away = ownerMap.Position - targetMap.Position;
        Vector2 dir;
        if (away.LengthSquared() < 0.01f)
            dir = new Vector2(1f, 0f);
        else
            dir = Vector2.Normalize(away);

        var retreatMap = new MapCoordinates(targetMap.Position + dir * RetreatDistance, targetMap.MapId);
        var retreatCoords = _transform.ToCoordinates((ownerXform.GridUid.Value, null), retreatMap);

        return (true, new Dictionary<string, object>
        {
            { RetreatCoordinatesKey, retreatCoords },
        });
    }
}
