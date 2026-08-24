// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Line;

public sealed class LineSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        _doorQuery = GetEntityQuery<DoorComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
    }

    public List<LineTile> DrawLine(
        EntityCoordinates start,
        EntityCoordinates end,
        TimeSpan delayPer,
        float? range,
        out EntityUid? blocker,
        bool hitBlocker = false,
        bool thick = false)
    {
        blocker = null;
        start = _mapSystem.AlignToGrid(start);

        end = _transform.WithEntityId(_mapSystem.AlignToGrid(end), start.EntityId);

        var tiles = new List<LineTile>();
        if (!start.TryDistance(EntityManager, _transform, end, out var distance))
            return tiles;

        if (range != null)
            distance = Math.Min(range.Value, distance);

        var distanceX = end.X - start.X;
        var distanceY = end.Y - start.Y;
        var x = start.X;
        var y = start.Y;
        var xOffset = distanceX / distance;
        var yOffset = distanceY / distance;
        var time = _timing.CurTime;
        var gridId = _transform.GetGrid(start.EntityId);
        var gridComp = gridId == null ? null : _mapGridQuery.CompOrNull(gridId.Value);
        Entity<MapGridComponent>? grid = gridComp == null ? null : new Entity<MapGridComponent>(gridId!.Value, gridComp);
        var lastCoords = start;

        for (var i = 0; i < distance; i++)
        {
            x += xOffset;
            y += yOffset;

            var center = new EntityCoordinates(start.EntityId, x, y).SnapToGrid(EntityManager, _mapManager);
            if (center == lastCoords)
                continue;

            List<EntityCoordinates> coords = new(9);
            coords.Add(center);

            if (thick && i > 1)
            {
                for (var xo = -1; xo < 2; xo++)
                {
                    for (var yo = -1; yo < 2; yo++)
                    {
                        if (xo == 0 && yo == 0)
                            continue;

                        var point = new EntityCoordinates(start.EntityId, x + xo, y + yo).SnapToGrid(EntityManager, _mapManager);
                        coords.Add(point);
                    }
                }
            }

            var centerBlocked = false;
            for (var j = 0; j < coords.Count; j++)
            {
                var entityCoords = coords[j];
                var blocked = IsTileBlocked(grid, lastCoords, entityCoords, out blocker);

                if (j == 0 && blocked && !hitBlocker)
                {
                    centerBlocked = true;
                    break;
                }

                var isDuplicate = false;
                foreach (var existing in tiles)
                {
                    if (existing.Coordinates.Position == entityCoords.Position)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    var delay = Vector2.Distance(entityCoords.Position, start.Position) - 1;
                    tiles.Add(new LineTile(entityCoords, time + delayPer * delay));
                }

                if (blocked && j == 0)
                {
                    centerBlocked = true;
                    break;
                }
            }

            if (centerBlocked)
                break;

            lastCoords = center;
        }

        return tiles;
    }

    private bool IsTileBlocked(
        Entity<MapGridComponent>? grid,
        EntityCoordinates previousCoords,
        EntityCoordinates coords,
        [NotNullWhen(true)] out EntityUid? blocker)
    {
        blocker = default;
        if (grid == null)
            return false;

        var previousMap = _transform.ToMapCoordinates(previousCoords);
        var currentMap = _transform.ToMapCoordinates(coords);
        var direction = currentMap.Position - previousMap.Position;
        if (direction != Vector2.Zero)
        {
            var ray = new CollisionRay(previousMap.Position, direction.Normalized(), (int) CollisionGroup.FullTileMask);
            var intersect = _physics.IntersectRayWithPredicate(
                previousMap.MapId,
                ray,
                direction.Length(),
                e => !Transform(e).Anchored,
                false);

            var results = intersect.Select(r => r.HitEntity).ToHashSet();
            var blockCount = 0;
            foreach (var entity in results)
            {
                if (!_tag.HasAnyTag(entity, StructureTag, WallTag))
                    continue;

                blockCount++;

                if (blockCount < 2)
                    continue;

                blocker = entity;
                return true;
            }
        }

        var indices = _mapSystem.TileIndicesFor(grid.Value, grid, coords);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid.Value, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (_doorQuery.TryComp(uid, out var door))
            {
                if (door.State != DoorState.Closed &&
                    door.State != DoorState.Denying &&
                    door.State != DoorState.Welded)
                {
                    continue;
                }

                blocker = uid.Value;
                return true;
            }

            if (_tag.HasAnyTag(uid.Value, StructureTag, WallTag))
            {
                blocker = uid.Value;
                return true;
            }
        }

        return false;
    }
}
