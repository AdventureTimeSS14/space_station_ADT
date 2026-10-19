using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.NPC.Helpers;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFollowLeaderOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Range = 12f;

    [DataField]
    public float FollowDistance = 4f;

    [DataField]
    public string Key = "ADTLeader";

    [DataField]
    public string KeyCoordinates = "ADTLeaderCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var leader = ADTNpcGroupHelper.FindLeader(_entManager, owner, Range);

        if (leader == owner)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        var transform = _entManager.System<SharedTransformSystem>();
        var distance = Vector2.Distance(
            transform.GetMapCoordinates(owner).Position,
            transform.GetMapCoordinates(leader).Position);

        if (distance <= FollowDistance)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, leader },
            { KeyCoordinates, new EntityCoordinates(leader, Vector2.Zero) },
        }));
    }
}
