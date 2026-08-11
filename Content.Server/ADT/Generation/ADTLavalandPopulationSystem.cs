using System.Linq;
using System.Numerics;
using Content.Server.ADT.Procedural;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Generation;

public sealed class ADTLavalandPopulationSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private readonly List<Vector2> _placed = new();
    private readonly List<(Vector2i Index, Tile Tile)> _tiles = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTLavalandPopulationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTLavalandPopulationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.PopulateAt = _timing.CurTime + ent.Comp.Delay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLavalandPopulationComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PopulateAt is not { } populateAt || now < populateAt)
                continue;

            comp.PopulateAt = null;
            Populate((uid, comp));
        }
    }

    public void Populate(Entity<ADTLavalandPopulationComponent> ent)
    {
        if (!TryComp<BiomeComponent>(ent, out var biome) || !TryComp<MapGridComponent>(ent, out var grid))
            return;

        var map = new Entity<BiomeComponent, MapGridComponent>(ent.Owner, biome, grid);

        _placed.Clear();

        foreach (var group in ent.Comp.Groups)
        {
            PopulateGroup(ent, map, group);
        }
    }

    private void PopulateGroup(
        Entity<ADTLavalandPopulationComponent> ent,
        Entity<BiomeComponent, MapGridComponent> map,
        ADTLavalandPopulationGroup group)
    {
        var totalWeight = group.Entries.Sum(entry => entry.Weight);
        if (group.Entries.Count == 0 || totalWeight <= 0f)
            return;

        var budget = _random.Next(group.MinBudget, group.MaxBudget + 1);
        var failures = 0;

        while (budget > 0 && failures <= group.MaxFailures)
        {
            var entry = Pick(group.Entries, totalWeight);

            if (entry.Cost > budget || !TryFindSpot(ent, map, group, out var indices))
            {
                failures++;
                continue;
            }

            failures = 0;
            budget -= entry.Cost;

            _placed.Add(new Vector2(indices.X, indices.Y));

            var center = new Vector2(indices.X + 0.5f, indices.Y + 0.5f);

            _tiles.Clear();
            _biome.ReserveTiles(ent.Owner, Box2.CenteredAround(center, new Vector2(0.8f, 0.8f)), _tiles);

            Spawn(PickPrototype(entry), new EntityCoordinates(ent.Owner, center));
        }
    }

    private ADTLavalandPopulationEntry Pick(List<ADTLavalandPopulationEntry> entries, float totalWeight)
    {
        var roll = _random.NextFloat(0f, totalWeight);

        foreach (var entry in entries)
        {
            roll -= entry.Weight;

            if (roll <= 0f)
                return entry;
        }

        return entries[^1];
    }

    private EntProtoId PickPrototype(ADTLavalandPopulationEntry entry)
    {
        foreach (var variant in entry.Variants)
        {
            if (_random.Prob(variant.Probability))
                return variant.Prototype;
        }

        return entry.Prototype;
    }

    private bool TryFindSpot(
        Entity<ADTLavalandPopulationComponent> ent,
        Entity<BiomeComponent, MapGridComponent> map,
        ADTLavalandPopulationGroup group,
        out Vector2i indices)
    {
        var center = ent.Comp.BaseCenter;
        var spacingSq = group.MinSpacing * group.MinSpacing;

        for (var attempt = 0; attempt < group.MaxAttempts; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(group.MinDistanceFromCenter, group.MaxRadius);
            var position = center + new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);

            indices = new Vector2i((int)MathF.Floor(position.X), (int)MathF.Floor(position.Y));

            if (!IsClearGround(map, indices))
                continue;

            var spot = new Vector2(indices.X, indices.Y);
            if (_placed.Any(other => Vector2.DistanceSquared(spot, other) < spacingSq))
                continue;

            if (group.AvoidRooms && IsNearRoom(ent, spot, group.RoomClearance))
                continue;

            return true;
        }

        indices = default;
        return false;
    }

    private bool IsNearRoom(Entity<ADTLavalandPopulationComponent> ent, Vector2 spot, float clearance)
    {
        var clearanceSq = clearance * clearance;

        if (TryComp<ADTLavalandGenerationComponent>(ent, out var generation) &&
            generation.Placed.Any(room => Vector2.DistanceSquared(spot, room) < clearanceSq))
        {
            return true;
        }

        var coords = new EntityCoordinates(ent.Owner, new Vector2(spot.X + 0.5f, spot.Y + 0.5f));

        return _lookup.GetEntitiesInRange(coords, clearance)
            .Any(e => HasComp<RoomFillComponent>(e) || HasComp<ADTRoomFillComponent>(e));
    }

    private bool IsClearGround(Entity<BiomeComponent, MapGridComponent> map, Vector2i indices)
    {
        var grid = new Entity<MapGridComponent>(map.Owner, map.Comp2);

        var anchored = _map.GetAnchoredEntitiesEnumerator(map.Owner, map.Comp2, indices);
        var occupied = anchored.MoveNext(out _);
        anchored.Dispose();

        if (occupied)
            return false;

        if (_map.TryGetTileRef(map.Owner, map.Comp2, indices, out var existing) && !existing.Tile.IsEmpty)
            return true;

        if (!_biome.TryGetBiomeTile(indices, map.Comp1.Layers, map.Comp1.Seed, grid, out var tile) ||
            tile.Value.IsEmpty)
        {
            return false;
        }

        return !_biome.TryGetEntity(indices, map.Comp1, grid, out _);
    }
}
