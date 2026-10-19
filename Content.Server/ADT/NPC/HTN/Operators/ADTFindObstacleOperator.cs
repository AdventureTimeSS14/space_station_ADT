using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTFindObstacleOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public const string ObstacleKey = "ADTObstacle";
    public const string ObstacleCoordinatesKey = "ADTObstacleCoordinates";

    [DataField]
    public float Range = 10f;

    [DataField]
    public float LineWidth = 0.7f;

    [DataField]
    public bool Openable;

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var transform = _entManager.System<SharedTransformSystem>();
        var interaction = _entManager.System<SharedInteractionSystem>();
        var lookup = _entManager.System<EntityLookupSystem>();
        var containers = _entManager.System<SharedContainerSystem>();
        var storageSystem = _entManager.System<SharedEntityStorageSystem>();
        var mobState = _entManager.System<MobStateSystem>();

        var ownerMap = transform.GetMapCoordinates(owner);
        EntityUid? best = null;
        var bestDist = float.MaxValue;

        foreach (var hostile in _entManager.System<NpcFactionSystem>().GetNearbyHostiles(owner, Range))
        {
            if (mobState.IsIncapacitated(hostile))
                continue;

            if (containers.TryGetContainingContainer(hostile, out var container))
            {
                if (!_entManager.HasComponent<EntityStorageComponent>(container.Owner))
                    continue;

                var canOpen = storageSystem.CanOpen(owner, container.Owner, true);

                if (canOpen != Openable)
                    continue;

                var d = Vector2.Distance(ownerMap.Position, transform.GetMapCoordinates(container.Owner).Position);

                if (d < bestDist)
                {
                    best = container.Owner;
                    bestDist = d;
                }

                continue;
            }

            if (Openable)
                continue;

            var hostileMap = transform.GetMapCoordinates(hostile);

            if (hostileMap.MapId != ownerMap.MapId)
                continue;

            if (interaction.InRangeUnobstructed(ownerMap, hostileMap, Range + 1f, predicate: e => e == owner || e == hostile))
                continue;

            var start = ownerMap.Position;
            var end = hostileMap.Position;
            var segment = end - start;
            var lengthSq = segment.LengthSquared();

            if (lengthSq < 0.01f)
                continue;

            foreach (var storage in lookup.GetEntitiesInRange<EntityStorageComponent>(ownerMap, Range))
            {
                var pos = transform.GetMapCoordinates(storage.Owner).Position;
                var along = Vector2.Dot(pos - start, segment) / lengthSq;

                if (along <= 0f || along >= 1f)
                    continue;

                var closest = start + segment * along;

                if (Vector2.Distance(closest, pos) > LineWidth)
                    continue;

                var blocker = storage.Owner;

                if (!interaction.InRangeUnobstructed(ownerMap, hostileMap, Range + 1f,
                        predicate: e => e == owner || e == hostile || e == blocker))
                {
                    continue;
                }

                var d = Vector2.Distance(start, pos);

                if (d < bestDist)
                {
                    best = storage.Owner;
                    bestDist = d;
                }
            }
        }

        if (best is not { } obstacle)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { ObstacleKey, obstacle },
            { ObstacleCoordinatesKey, new EntityCoordinates(obstacle, Vector2.Zero) },
        }));
    }
}
