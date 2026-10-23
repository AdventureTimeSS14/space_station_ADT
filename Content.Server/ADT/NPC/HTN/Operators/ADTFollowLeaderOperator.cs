using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFollowLeaderOperator : HTNOperator
{
    private ADTNpcCombatMemorySystem _memory = default!;
    private SharedTransformSystem _transform = default!;

    [DataField]
    public float Range = 12f;

    [DataField]
    public float FollowDistance = 4f;

    [DataField]
    public string Key = "ADTLeader";

    [DataField]
    public string KeyCoordinates = "ADTLeaderCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _memory = sysManager.GetEntitySystem<ADTNpcCombatMemorySystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var leader = _memory.FindLeader(owner, Range);

        if (leader == owner || _transform.InRange(owner, leader, FollowDistance))
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, leader },
            { KeyCoordinates, new EntityCoordinates(leader, Vector2.Zero) },
        }));
    }
}
