using Content.Server.NPC.Pathfinding;
using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Security.Components;
using Content.Shared.Security;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyContrabandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedAudioSystem _audio = default!;

    [DataField]
    public float MinContrabandPoints = 3f;

    [DataField]
    public string RangeKey = NPCBlackboard.SecuritronArrestRange;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    [DataField]
    public string? TargetFoundSoundKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        var cuffableQuery = _entManager.GetEntityQuery<CuffableComponent>();
        var criminalRecordQuery = _entManager.GetEntityQuery<CriminalRecordComponent>();
        var mobStateQuery = _entManager.GetEntityQuery<MobStateComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(owner, range))
        {
            if (!criminalRecordQuery.TryGetComponent(entity, out var criminalRecord))
                continue;

            var contrabandPoints = criminalRecord.Points;
            if (criminalRecord.Status != SecurityStatus.None)
            {
                contrabandPoints -= criminalRecord.SecurityStatusPoints[criminalRecord.Status];
            }

            if (contrabandPoints < MinContrabandPoints)
                continue;

            if (!mobStateQuery.TryGetComponent(entity, out var state) || state.CurrentState != MobState.Alive)
                continue;

            if (cuffableQuery.TryGetComponent(entity, out var cuffable) && cuffable.CuffedHandCount > 0)
                continue;

            if (criminalRecord.Points >= 10f)
                continue;

            var pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

            if (path.Result == PathResult.NoPath)
                continue;

            if (TargetFoundSoundKey != null &&
                (!blackboard.TryGetValue<EntityUid>(TargetKey, out var oldTarget, _entManager) ||
                 oldTarget != entity) &&
                blackboard.TryGetValue<SoundSpecifier>(TargetFoundSoundKey, out var targetFoundSound, _entManager))
            {
                _audio.PlayPvs(targetFoundSound, owner);
            }

            return (true, new Dictionary<string, object>()
            {
                {TargetKey, entity},
                {TargetMoveKey, _entManager.GetComponent<TransformComponent>(entity).Coordinates},
                {NPCBlackboard.PathfindKey, path},
            });
        }

        return (false, null);
    }
}
