using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Map;

namespace Content.Server.ADT.ZombieJump.Preconditions;
public sealed partial class ZombieJumpRangePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SharedTransformSystem _transformSystem = default!;

    [DataField("targetKey", required: true)] public string TargetKey = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transformSystem = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        try
        {
            if (!blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates, _entManager))
                return false;

            if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
                return false;

            if (!_entManager.TryGetComponent<TransformComponent>(target, out var targetXform))
                return false;

            var jumpRange = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
            return _transformSystem.InRange(coordinates, targetXform.Coordinates, jumpRange);
        }
        catch
        {
            return false;
        }
    }
}
