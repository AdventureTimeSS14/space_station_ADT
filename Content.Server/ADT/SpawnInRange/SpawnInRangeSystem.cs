using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.Random;
using Content.Server.Procedural;
using System.Linq;

namespace Content.Server.ADT.Generation;

public sealed partial class SpawnInRangeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnInRangeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SpawnInRangeComponent component, MapInitEvent args)
    {
        foreach (var spawned in component.ProtosToSpawn)
        {
            var coords = TryGetCords(uid, component.MaxX, component.MaxY, component.MinX, component.MinY);
            while (RetryCoords(coords, component.ClearRadiusAroundSpawned)) // тут просто перепроверяет координаты, чтобы не спавнились данжи в данжах и данжи в аванпосте
            {
                coords = TryGetCords(uid, component.MaxX, component.MaxY, component.MinX, component.MinY);
            }
            Spawn(spawned, coords);
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
        if (coords.X >= -40 && coords.X <= 40 || coords.Y >= -40 && coords.Y <= 40) return false; //чутка щиткода, чтобы данжи не спавнились внутри аванпоста
        return _lookup.GetEntitiesInRange(coords, clearRadiusAroundSpawned).Any(lookup => HasComp<RoomFillComponent>(lookup));
    }
}
