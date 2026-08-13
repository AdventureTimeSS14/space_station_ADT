using System.Linq;
using System.Numerics;
using Content.Server.ADT.Generation;
using Content.Server.ADT.Procedural;
using Content.Server.Procedural;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class ADTMegafaunaSpawnSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly List<(EntProtoId Proto, EntityCoordinates Coords)> _pendingSpawns = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMegafaunaSpawnComponent, MapInitEvent>(OnMapInit,
            after: [typeof(ADTLavalandGenerationSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingSpawns.Count == 0)
            return;

        var toSpawn = new List<(EntProtoId Proto, EntityCoordinates Coords)>(_pendingSpawns);
        _pendingSpawns.Clear();

        foreach (var (proto, coords) in toSpawn)
        {
            if (!coords.IsValid(EntityManager))
                continue;

            Spawn(proto, coords);
        }
    }

    private void OnMapInit(Entity<ADTMegafaunaSpawnComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (comp.Bosses.Count == 0 || comp.Count <= 0)
            return;

        var pool = comp.Bosses.ToList();
        _random.Shuffle(pool);
        var toSpawn = Math.Min(comp.Count, pool.Count);

        var placed = new List<Vector2>();

        for (var i = 0; i < toSpawn; i++)
        {
            if (!TryFindSpot(ent, placed, out var coords))
            {
                Log.Warning($"Found nowhere to put megafauna {pool[i]} on {ToPrettyString(ent.Owner)}.");
                continue;
            }

            _pendingSpawns.Add((pool[i], coords));
            placed.Add(coords.Position);
        }
    }

    private bool TryFindSpot(Entity<ADTMegafaunaSpawnComponent> ent, List<Vector2> placed, out EntityCoordinates coords)
    {
        var comp = ent.Comp;

        for (var attempt = 0; attempt < comp.MaxAttempts; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(comp.MinDistanceFromCenter, comp.MaxRadius);
            var offset = new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);

            coords = new EntityCoordinates(ent.Owner, comp.BaseCenter + offset);

            if (IsValidSpot(ent, coords, placed))
                return true;
        }

        coords = default;
        return false;
    }

    private bool IsValidSpot(Entity<ADTMegafaunaSpawnComponent> ent, EntityCoordinates coords, List<Vector2> placed)
    {
        var comp = ent.Comp;
        var spacingSq = comp.MinSpacing * comp.MinSpacing;

        foreach (var other in placed)
        {
            if (Vector2.DistanceSquared(coords.Position, other) < spacingSq)
                return false;
        }

        if (!comp.AvoidRooms)
            return true;

        var clearanceSq = comp.RoomClearance * comp.RoomClearance;

        if (TryComp<ADTLavalandGenerationComponent>(ent, out var generation) &&
            generation.Placed.Any(room => Vector2.DistanceSquared(coords.Position, room) < clearanceSq))
        {
            return false;
        }

        return !_lookup.GetEntitiesInRange(coords, comp.RoomClearance)
            .Any(e => HasComp<RoomFillComponent>(e) || HasComp<ADTRoomFillComponent>(e));
    }
}
