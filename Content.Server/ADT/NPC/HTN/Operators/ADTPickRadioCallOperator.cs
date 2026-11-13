using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTPickRadioCallOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField(required: true)]
    public List<string> Types = new();

    [DataField]
    public float MaxAge = 30f;

    [DataField]
    public float MinDistance = 6f;

    [DataField]
    public float MaxDistance = 40f;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_entManager.System<ADTNpcRadioSystem>().FindCall(owner, Types, MaxAge, MinDistance, MaxDistance) is not { } call)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        var coords = _entManager.System<SharedTransformSystem>().ToCoordinates(call.Position);

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { TargetCoordinates, coords },
        }));
    }
}
