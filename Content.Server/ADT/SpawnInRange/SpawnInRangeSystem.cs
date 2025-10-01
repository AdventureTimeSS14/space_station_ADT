using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.Random;
using Content.Server.Procedural;
using System.Linq;
using System.Collections.Generic;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.Generation;

public sealed partial class SpawnInRangeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    private const int MaxAttempts = 1000;
    private const float MinDistanceFromOrigin = 40f;
    private const float MinDistanceBetweenSpawns = 20f;

    private readonly List<Vector2> _spawnedPositions = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnInRangeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SpawnInRangeComponent component, MapInitEvent args)
    {
        _spawnedPositions.Clear();

        if (component.ProtosToSpawn == null || component.ProtosToSpawn.Count == 0)
            return;

        foreach (var protoId in component.ProtosToSpawn)
        {
            if (TryFindValidSpawnPosition(uid, component, out var coords))
            {
                Spawn(protoId, coords);
                _spawnedPositions.Add(coords.Position);
            }
        }

        if (component.SendFaxCoords && TryComp<TransformComponent>(uid, out var mapform))
        {
            var coordstText = "";
            foreach (var coord in _spawnedPositions)
            {
                coordstText += coord.ToString() + Environment.NewLine;
            }
            var printout = new FaxPrintout(
                Loc.GetString("paper-lava-scan-start") + coordstText,
                Loc.GetString("lava-fax-paper-name"),
                null,
                null,
                "paper_stamp-centcom",
                [new() { StampedName = Loc.GetString("stamp-component-stamped-name-lava-scan"), StampedColor = Color.FromHex("#0766a5ff") }]
            );
            Timer.Spawn(3000, () =>
            {
                var query = EntityQueryEnumerator<FaxMachineComponent>();
                while (query.MoveNext(out var faxUid, out var fax))
                {
                    if (TryComp<TransformComponent>(faxUid, out var faxform) && faxform.MapID == mapform.MapID)
                    {
                        _fax.Receive(faxUid, printout, null, fax);
                    }
                }
            });
        }
    }

    private bool TryFindValidSpawnPosition(EntityUid uid, SpawnInRangeComponent component, out EntityCoordinates coords)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            coords = GenerateRandomCoords(uid, component.MaxX, component.MaxY, component.MinX, component.MinY);

            if (IsValidSpawnPosition(coords, component.ClearRadiusAroundSpawned))
            {
                return true;
            }
        }

        coords = default;
        return false;
    }

    private EntityCoordinates GenerateRandomCoords(EntityUid uid, float maxX, float maxY, float minX, float minY)
    {
        var angle = _random.NextFloat(0, MathF.PI * 2);
        var distance = _random.NextFloat(minX, maxX);

        var x = MathF.Cos(angle) * distance;
        var y = MathF.Sin(angle) * distance;

        return new EntityCoordinates(uid, x, y);
    }

    private bool IsValidSpawnPosition(EntityCoordinates coords, float clearRadius)
    {
        var position = coords.Position;

        var distanceFromOriginSq = position.LengthSquared();
        if (distanceFromOriginSq < MinDistanceFromOrigin * MinDistanceFromOrigin)
            return false;

        foreach (var spawnedPos in _spawnedPositions)
        {
            var distanceSq = Vector2.DistanceSquared(position, spawnedPos);
            if (distanceSq < MinDistanceBetweenSpawns * MinDistanceBetweenSpawns)
                return false;
        }

        return !_lookup.GetEntitiesInRange(coords, clearRadius)
                       .Any(entity => HasComp<RoomFillComponent>(entity));
    }
}
