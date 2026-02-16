using Content.Server.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Is the specified key within the specified range of us.
/// </summary>
public sealed partial class TargetInRangePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedTransformSystem _transformSystem = default!;
    private StealthSystem _stealth = default!; // ADT-Tweak

    [DataField("targetKey", required: true)] public string TargetKey = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = default!;
    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _transformSystem = sysManager.GetEntitySystem<SharedTransformSystem>();
        _stealth = sysManager.GetEntitySystem<StealthSystem>(); // ADT-Tweak
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates, _entManager))
            return false;

        // ADT-Tweak-Start
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager)
        || !_entManager.TryGetComponent<TransformComponent>(target, out var targetXform)
        || _entManager.TryGetComponent<StealthComponent>(target, out var stealth) && _stealth.GetVisibility(target, stealth) <= stealth.ExamineThreshold)
            return false;
        // ADT-Tweak-End

        var transformSystem = _entManager.System<SharedTransformSystem>;
        return _transformSystem.InRange(coordinates, targetXform.Coordinates, blackboard.GetValueOrDefault<float>(RangeKey, _entManager));
    }
}
