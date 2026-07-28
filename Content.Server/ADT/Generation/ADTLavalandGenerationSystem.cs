using System.Linq;
using System.Numerics;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation;

public sealed class ADTLavalandGenerationSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly List<(EntProtoId Proto, EntityCoordinates Coords)> _pendingSpawns = new();
    private readonly List<(Vector2i Index, Tile Tile)> _reservedTiles = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTLavalandGenerationComponent, MapInitEvent>(OnMapInit);
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

    public void ReserveSafeZone(Entity<ADTLavalandGenerationComponent> ent, Vector2 center)
    {
        var comp = ent.Comp;

        var seed = TryComp<BiomeComponent>(ent, out var biome) ? biome.Seed : _random.Next();

        var edgeNoise = new FastNoiseLite(seed);
        edgeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        edgeNoise.SetFrequency(comp.SafeEdgeFrequency);

        var falloffNoise = new FastNoiseLite(seed + 1);
        falloffNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        falloffNoise.SetFrequency(comp.SafeFalloffFrequency);

        var outer = comp.SafeRadius + comp.SafeEdgeNoise + comp.SafeFalloff;
        var minX = (int)MathF.Floor(center.X - outer);
        var minY = (int)MathF.Floor(center.Y - outer);
        var maxX = (int)MathF.Ceiling(center.X + outer);
        var maxY = (int)MathF.Ceiling(center.Y + outer);

        for (var y = minY; y <= maxY; y++)
        {
            var runStart = int.MinValue;

            for (var x = minX; x <= maxX + 1; x++)
            {
                var clear = x <= maxX && IsSafeTile(comp, edgeNoise, falloffNoise, center, x, y);

                if (clear)
                {
                    if (runStart == int.MinValue)
                        runStart = x;

                    continue;
                }

                if (runStart == int.MinValue)
                    continue;

                ReserveRow(ent, runStart, x - 1, y);
                runStart = int.MinValue;
            }
        }
    }

    private bool IsSafeTile(
        ADTLavalandGenerationComponent comp,
        FastNoiseLite edgeNoise,
        FastNoiseLite falloffNoise,
        Vector2 center,
        int x,
        int y)
    {
        var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
        var border = comp.SafeRadius + edgeNoise.GetNoise(x, y) * comp.SafeEdgeNoise;

        if (distance <= border)
            return true;

        if (comp.SafeFalloff <= 0f || distance > border + comp.SafeFalloff)
            return false;

        var depth = (distance - border) / comp.SafeFalloff;
        var patch = (falloffNoise.GetNoise(x, y) + 1f) * 0.5f;

        return patch > depth;
    }

    private void ReserveRow(EntityUid map, int fromX, int toX, int y)
    {
        _reservedTiles.Clear();

        var bounds = new Box2(fromX + 0.1f, y + 0.1f, toX + 0.9f, y + 0.9f);
        _biome.ReserveTiles(map, bounds, _reservedTiles);
    }

    private void OnMapInit(Entity<ADTLavalandGenerationComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        var placed = comp.Placed;
        placed.Clear();

        foreach (var group in comp.Groups)
        {
            if (group.Prototypes.Count == 0 || group.Count <= 0)
                continue;

            for (var i = 0; i < group.Count; i++)
            {
                if (!TryFindSpot(ent, group, placed, out var coords))
                    continue;

                var proto = _random.Pick(group.Prototypes);
                _pendingSpawns.Add((proto, coords));

                placed.Add(coords.Position);
            }
        }
    }

    private bool TryFindSpot(
        Entity<ADTLavalandGenerationComponent> ent,
        LavalandScatterGroup group,
        List<Vector2> placed,
        out EntityCoordinates coords)
    {
        var comp = ent.Comp;
        var minCenter = MathF.Max(group.MinDistanceFromCenter, comp.MinRadius);

        for (var attempt = 0; attempt < comp.MaxAttempts; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(minCenter, comp.MaxRadius);
            var offset = new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);

            coords = new EntityCoordinates(ent.Owner, comp.BaseCenter + offset);

            if (IsValidSpot(coords, group, placed))
                return true;
        }

        coords = default;
        return false;
    }

    private bool IsValidSpot(EntityCoordinates coords, LavalandScatterGroup group, List<Vector2> placed)
    {
        var position = coords.Position;

        var spacingSq = group.MinSpacing * group.MinSpacing;
        foreach (var other in placed)
        {
            if (Vector2.DistanceSquared(position, other) < spacingSq)
                return false;
        }

        if (group.AvoidRooms &&
            _lookup.GetEntitiesInRange(coords, group.MinSpacing).Any(e => HasComp<RoomFillComponent>(e)))
        {
            return false;
        }

        return true;
    }
}
