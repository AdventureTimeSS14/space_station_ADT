using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindCritEnemyOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Range = 5f;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var mobState = _entManager.System<MobStateSystem>();
        var transform = _entManager.System<SharedTransformSystem>();
        var containers = _entManager.System<SharedContainerSystem>();
        var ownerPos = transform.GetMapCoordinates(owner).Position;

        EntityUid? best = null;
        var bestDist = float.MaxValue;

        foreach (var hostile in _entManager.System<NpcFactionSystem>().GetNearbyHostiles(owner, Range))
        {
            if (!mobState.IsCritical(hostile) || containers.IsEntityInContainer(hostile))
                continue;

            var dist = Vector2.Distance(ownerPos, transform.GetMapCoordinates(hostile).Position);

            if (dist >= bestDist)
                continue;

            best = hostile;
            bestDist = dist;
        }

        if (best is not { } target)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
        }));
    }
}
