using System.Linq;
using System.Numerics;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Noise;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation;

public sealed partial class ADTLavalandGenerationSystem
{
    private const float RiverTurnRate = 0.12f;
    private const float EdgeNoiseFrequency = 0.15f;
    private const int BridgeBankDepth = 3;
    private const int BridgePathCheck = 4;

    private readonly HashSet<Vector2i> _lavaTiles = new();
    private readonly HashSet<Vector2i> _lakeTiles = new();
    private readonly HashSet<Vector2i> _islandTiles = new();
    private readonly HashSet<Vector2i> _shoreTiles = new();
    private readonly HashSet<Vector2i> _bridgeTiles = new();
    private readonly List<List<(Vector2 Position, float Width)>> _riverPaths = new();

    private void GenerateRivers(Entity<ADTLavalandGenerationComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.RiverEntity is not { } riverEntity)
            return;

        _lavaTiles.Clear();
        _lakeTiles.Clear();
        _islandTiles.Clear();
        _shoreTiles.Clear();
        _bridgeTiles.Clear();
        _riverPaths.Clear();

        var seed = TryComp<BiomeComponent>(ent, out var biome) ? biome.Seed : _random.Next();
        var edgeNoise = new FastNoiseLite(seed + 2);
        edgeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        edgeNoise.SetFrequency(EdgeNoiseFrequency);

        if (comp.RiverNodes >= 2)
        {
            var nodes = new List<Vector2>();
            var sector = MathF.PI * 2 / comp.RiverNodes;
            var angleOffset = _random.NextFloat(0, MathF.PI * 2);
            for (var i = 0; i < comp.RiverNodes; i++)
            {
                var angle = angleOffset + sector * (i + _random.NextFloat(0.15f, 0.85f));
                nodes.Add(RandomRingPoint(comp, angle));
            }

            var connected = new HashSet<(int, int)>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var from = i;
                var nearest = Enumerable.Range(0, nodes.Count)
                    .Where(j => j != from)
                    .OrderBy(j => Vector2.DistanceSquared(nodes[from], nodes[j]))
                    .Take(2);

                foreach (var targetIndex in nearest)
                {
                    if (!connected.Add((Math.Min(from, targetIndex), Math.Max(from, targetIndex))))
                        continue;

                    var path = BuildRiverPath(comp, nodes[from], nodes[targetIndex]);
                    _riverPaths.Add(path);

                    foreach (var (position, width) in path)
                    {
                        StampBlob(comp, edgeNoise, position, width, comp.RiverEdgeNoise, _lavaTiles, null);
                    }
                }
            }
        }

        for (var i = 0; i < comp.Lakes; i++)
        {
            PlaceLake(comp, edgeNoise);
        }

        SmoothLava(comp);
        BuildShore(comp, edgeNoise);
        PlaceBridges(comp);

        ReserveTileSet(ent, _lavaTiles.Concat(_shoreTiles));

        foreach (var tile in _lavaTiles)
        {
            _pendingSpawns.Add((riverEntity, TileCoordinates(ent, tile)));
        }

        if (comp.RiverBridge is not { } bridge)
            return;

        foreach (var tile in _bridgeTiles)
        {
            _pendingSpawns.Add((bridge, TileCoordinates(ent, tile)));
        }
    }

    private List<(Vector2 Position, float Width)> BuildRiverPath(ADTLavalandGenerationComponent comp, Vector2 start, Vector2 target)
    {
        var path = new List<(Vector2 Position, float Width)>();
        var position = start;
        var heading = MathF.Atan2(target.Y - start.Y, target.X - start.X);
        var meanderPhase = _random.NextFloat(0f, 100f);
        var widthPhase = _random.NextFloat(0f, 100f);
        var maxSteps = (int) (Vector2.Distance(start, target) * 3f) + 50;

        for (var step = 0; step < maxSteps; step++)
        {
            var remaining = Vector2.Distance(position, target);
            if (remaining <= 3f)
                break;

            var approach = Math.Clamp(remaining / 30f, 0f, 1f);
            var turnRate = RiverTurnRate * (2f - approach);

            var toTarget = MathF.Atan2(target.Y - position.Y, target.X - position.X);
            var meander = approach * comp.RiverMeander * (MathF.Sin(step * 0.045f + meanderPhase) + 0.45f * MathF.Sin(step * 0.11f + meanderPhase * 2f));
            var turn = WrapAngle(toTarget + meander - heading);

            heading += Math.Clamp(turn, -turnRate, turnRate);
            position += new Vector2(MathF.Cos(heading), MathF.Sin(heading));

            var widthWave = 0.5f + 0.5f * MathF.Sin(step * 0.03f + widthPhase);
            var width = comp.RiverMinWidth + (comp.RiverMaxWidth - comp.RiverMinWidth) * widthWave + 0.4f * MathF.Sin(step * 0.13f + widthPhase * 2f);
            path.Add((position, MathF.Max(0.8f, width)));
        }

        return path;
    }

    private void PlaceLake(ADTLavalandGenerationComponent comp, FastNoiseLite edgeNoise)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var radius = _random.NextFloat(comp.LakeMinRadius, comp.LakeMaxRadius);
            Vector2 center;

            if (_riverPaths.Count > 0 && _random.Prob(comp.LakeOnRiverChance))
            {
                var path = _random.Pick(_riverPaths);
                if (path.Count == 0)
                    continue;

                center = _random.Pick(path).Position;
            }
            else
            {
                center = RandomRingPoint(comp, _random.NextFloat(0, MathF.PI * 2));
            }

            if (IsLakeBlocked(comp, center, radius))
                continue;

            StampBlob(comp, edgeNoise, center, radius, radius * 0.45f, _lavaTiles, _lakeTiles);

            if (radius >= comp.LakeMinRadius + (comp.LakeMaxRadius - comp.LakeMinRadius) * 0.4f && _random.Prob(comp.LakeIslandChance))
            {
                var islandCenter = center + _random.NextVector2(radius * 0.3f);
                var islandRadius = radius * _random.NextFloat(0.22f, 0.32f);
                StampBlob(comp, edgeNoise, islandCenter, islandRadius, islandRadius * 0.3f, _islandTiles, null);
            }

            return;
        }
    }

    private bool IsLakeBlocked(ADTLavalandGenerationComponent comp, Vector2 center, float radius)
    {
        var fromBase = Vector2.Distance(center, comp.BaseCenter);

        if (fromBase - radius <= comp.SafeRadius + comp.SafeEdgeNoise + comp.SafeFalloff)
            return true;

        if (fromBase + radius >= comp.MaxRadius)
            return true;

        var clearance = comp.RiverRoomClearance + radius;
        foreach (var room in comp.Placed)
        {
            if (Vector2.DistanceSquared(center, room) < clearance * clearance)
                return true;
        }

        return false;
    }

    private void StampBlob(
        ADTLavalandGenerationComponent comp,
        FastNoiseLite edgeNoise,
        Vector2 center,
        float radius,
        float noise,
        HashSet<Vector2i> target,
        HashSet<Vector2i>? secondTarget)
    {
        var reach = radius + noise + 1f;
        var minX = (int) MathF.Floor(center.X - reach);
        var maxX = (int) MathF.Ceiling(center.X + reach);
        var minY = (int) MathF.Floor(center.Y - reach);
        var maxY = (int) MathF.Ceiling(center.Y + reach);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var tile = new Vector2i(x, y);
                if (secondTarget == null && target.Contains(tile))
                    continue;

                var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (distance >= radius + noise * edgeNoise.GetNoise(x, y))
                    continue;

                if (IsRiverBlocked(comp, tile))
                    continue;

                target.Add(tile);
                secondTarget?.Add(tile);
            }
        }
    }

    private void SmoothLava(ADTLavalandGenerationComponent comp)
    {
        _lavaTiles.ExceptWith(_islandTiles);

        if (_lavaTiles.Count == 0)
            return;

        var minX = _lavaTiles.Min(t => t.X) - 1;
        var minY = _lavaTiles.Min(t => t.Y) - 1;
        var width = _lavaTiles.Max(t => t.X) - minX + 2;
        var height = _lavaTiles.Max(t => t.Y) - minY + 2;

        var grid = new bool[width, height];
        foreach (var tile in _lavaTiles)
        {
            grid[tile.X - minX, tile.Y - minY] = true;
        }

        for (var pass = 0; pass < comp.RiverSmoothPasses; pass++)
        {
            var next = new bool[width, height];

            for (var x = 1; x < width - 1; x++)
            {
                for (var y = 1; y < height - 1; y++)
                {
                    var neighbours = 0;
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            if ((dx != 0 || dy != 0) && grid[x + dx, y + dy])
                                neighbours++;
                        }
                    }

                    if (neighbours >= 5)
                        next[x, y] = true;
                    else if (neighbours <= 2)
                        next[x, y] = false;
                    else
                        next[x, y] = grid[x, y];
                }
            }

            grid = next;
        }

        _lavaTiles.Clear();
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (!grid[x, y])
                    continue;

                var tile = new Vector2i(x + minX, y + minY);
                if (_islandTiles.Contains(tile) || IsRiverBlocked(comp, tile))
                    continue;

                _lavaTiles.Add(tile);
            }
        }

        _lakeTiles.IntersectWith(_lavaTiles);
    }

    private void BuildShore(ADTLavalandGenerationComponent comp, FastNoiseLite edgeNoise)
    {
        foreach (var tile in _lavaTiles)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    var shore = new Vector2i(tile.X + dx, tile.Y + dy);
                    if (_lavaTiles.Contains(shore) || _shoreTiles.Contains(shore))
                        continue;

                    var outer = Math.Abs(dx) == 2 || Math.Abs(dy) == 2;
                    if (outer && edgeNoise.GetNoise(shore.X * 3, shore.Y * 3) < 0.2f)
                        continue;

                    if (IsRiverBlocked(comp, shore))
                        continue;

                    _shoreTiles.Add(shore);
                }
            }
        }
    }

    private void PlaceBridges(ADTLavalandGenerationComponent comp)
    {
        if (comp.RiverBridge == null || comp.RiverBridgeSpacing <= 0f)
            return;

        foreach (var path in _riverPaths)
        {
            var sinceLast = comp.RiverBridgeSpacing * 0.5f;

            for (var i = BridgePathCheck; i < path.Count - BridgePathCheck; i++)
            {
                sinceLast += 1f;
                if (sinceLast < comp.RiverBridgeSpacing)
                    continue;

                if (TryPlaceBridge(comp, path, i))
                    sinceLast = 0f;
            }
        }
    }

    private bool TryPlaceBridge(ADTLavalandGenerationComponent comp, List<(Vector2 Position, float Width)> path, int index)
    {
        var center = ToTile(path[index].Position);

        if (!_lavaTiles.Contains(center) || _lakeTiles.Contains(center))
            return false;

        for (var offset = -BridgePathCheck; offset <= BridgePathCheck; offset++)
        {
            var tile = ToTile(path[index + offset].Position);
            if (!_lavaTiles.Contains(tile) || _lakeTiles.Contains(tile))
                return false;
        }

        var flow = path[index + 2].Position - path[index - 2].Position;
        var axis = MathF.Abs(flow.X) >= MathF.Abs(flow.Y) ? new Vector2i(0, 1) : new Vector2i(1, 0);
        var across = new Vector2i(axis.Y, axis.X);

        if (!TryFindBank(comp, center, -axis, out var from) || !TryFindBank(comp, center, axis, out var to))
            return false;

        var lavaSpan = Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y) - 1;
        if (lavaSpan > comp.RiverBridgeMaxWidth)
            return false;

        for (var tile = from + axis; tile != to; tile += axis)
        {
            if (_lakeTiles.Contains(tile))
                return false;
        }

        for (var tile = from + axis * 2; tile != to - axis && tile != to; tile += axis)
        {
            if (!_lavaTiles.Contains(tile + across) || !_lavaTiles.Contains(tile - across))
                return false;
        }

        for (var tile = from; tile != to + axis; tile += axis)
        {
            _bridgeTiles.Add(tile);
        }

        return true;
    }

    private bool TryFindBank(ADTLavalandGenerationComponent comp, Vector2i center, Vector2i direction, out Vector2i bank)
    {
        bank = center;

        for (var step = 1; step <= comp.RiverBridgeMaxWidth + 1; step++)
        {
            bank = center + direction * step;

            if (_lavaTiles.Contains(bank))
                continue;

            for (var depth = 0; depth < BridgeBankDepth; depth++)
            {
                var land = bank + direction * depth;
                if (_lavaTiles.Contains(land) || IsRiverBlocked(comp, land))
                    return false;
            }

            return true;
        }

        return false;
    }

    private Vector2 RandomRingPoint(ADTLavalandGenerationComponent comp, float angle)
    {
        var min = comp.RiverMinRadius * comp.RiverMinRadius;
        var max = comp.RiverMaxRadius * comp.RiverMaxRadius;
        var distance = MathF.Sqrt(_random.NextFloat(min, max));
        return comp.BaseCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    private static Vector2i ToTile(Vector2 position)
    {
        return new Vector2i((int) MathF.Floor(position.X), (int) MathF.Floor(position.Y));
    }

    private bool IsRiverBlocked(ADTLavalandGenerationComponent comp, Vector2i tile)
    {
        var center = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
        var fromBase = Vector2.Distance(center, comp.BaseCenter);

        if (fromBase <= comp.SafeRadius + comp.SafeEdgeNoise + comp.SafeFalloff)
            return true;

        if (fromBase >= comp.MaxRadius)
            return true;

        var clearanceSq = comp.RiverRoomClearance * comp.RiverRoomClearance;
        foreach (var room in comp.Placed)
        {
            if (Vector2.DistanceSquared(center, room) < clearanceSq)
                return true;
        }

        return false;
    }

    private void ReserveTileSet(EntityUid map, IEnumerable<Vector2i> tiles)
    {
        var ordered = tiles.OrderBy(t => t.Y).ThenBy(t => t.X).ToList();
        if (ordered.Count == 0)
            return;

        var runStart = ordered[0];
        var previous = ordered[0];

        for (var i = 1; i <= ordered.Count; i++)
        {
            if (i < ordered.Count && ordered[i].Y == previous.Y && ordered[i].X == previous.X + 1)
            {
                previous = ordered[i];
                continue;
            }

            ReserveRow(map, runStart.X, previous.X, runStart.Y);

            if (i == ordered.Count)
                break;

            runStart = ordered[i];
            previous = ordered[i];
        }
    }

    private static EntityCoordinates TileCoordinates(EntityUid map, Vector2i tile)
    {
        return new EntityCoordinates(map, new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
    }

    private static float WrapAngle(float angle)
    {
        var wrapped = (angle + MathF.PI) % (MathF.PI * 2f);
        if (wrapped < 0f)
            wrapped += MathF.PI * 2f;

        return wrapped - MathF.PI;
    }
}
