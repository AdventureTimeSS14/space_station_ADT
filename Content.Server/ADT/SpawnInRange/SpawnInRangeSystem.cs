using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.Random;
using Content.Server.Procedural;
using System.Linq;
using System.Collections.Generic;

namespace Content.Server.ADT.Generation;

public sealed partial class SpawnInRangeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly List<EntityCoordinates> _spawnedPositions = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnInRangeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SpawnInRangeComponent component, MapInitEvent args)
    {
        _spawnedPositions.Clear();

        foreach (var spawned in component.ProtosToSpawn)
        {
            var coords = TryGetCords(uid, component.MaxX, component.MaxY, component.MinX, component.MinY);
            int attempts = 0;
            const int maxAttempts = 1000;

            while (RetryCoords(coords, component.ClearRadiusAroundSpawned) && attempts < maxAttempts)
            {
                coords = TryGetCords(uid, component.MaxX, component.MaxY, component.MinX, component.MinY);
                attempts++;
            }

            if (attempts < maxAttempts)
            {
                Spawn(spawned, coords);
                _spawnedPositions.Add(coords);
            }
        }
    }

    private EntityCoordinates TryGetCords(EntityUid uid, float maxX, float maxY, float minX, float minY)
    {
        var randomX = _random.NextFloat(minX, maxX);
        var randomY = _random.NextFloat(minY, maxY);
        var coords1 = new Vector2(randomX, randomY);
        var coords = new EntityCoordinates(uid, coords1);
        return coords;
    }

    private bool RetryCoords(EntityCoordinates coords, float clearRadiusAroundSpawned)
    {

        var distanceFromOrigin = Math.Sqrt(coords.X * coords.X + coords.Y * coords.Y);
        if (distanceFromOrigin < 40)
            return true;

        foreach (var spawnedPos in _spawnedPositions)
        {
            var dx = coords.X - spawnedPos.X;
            var dy = coords.Y - spawnedPos.Y;
            var distanceFromSpawned = Math.Sqrt(dx * dx + dy * dy);

            if (distanceFromSpawned < 20)
                return true;
        }

        return _lookup.GetEntitiesInRange(coords, clearRadiusAroundSpawned)
                     .Any(lookup => HasComp<RoomFillComponent>(lookup));
    }
}