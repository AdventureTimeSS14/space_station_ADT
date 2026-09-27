using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation;

public sealed partial class ADTLavalandGenerationSystem
{
    private static readonly Vector2i[] RiverDirections =
    {
        new(0, 1),
        new(1, 1),
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, -1),
        new(-1, 0),
        new(-1, 1),
    };

    private readonly HashSet<Vector2i> _riverTiles = new();
    private readonly HashSet<Vector2i> _shoreTiles = new();

    private void GenerateRivers(Entity<ADTLavalandGenerationComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.RiverNodes < 2 || comp.RiverEntity is not { } riverEntity)
            return;

        _riverTiles.Clear();
        _shoreTiles.Clear();

        var nodes = new List<Vector2i>();
        for (var i = 0; i < comp.RiverNodes; i++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(comp.RiverMinRadius, comp.RiverMaxRadius);
            var position = comp.BaseCenter + new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);

            nodes.Add(new Vector2i((int)MathF.Floor(position.X), (int)MathF.Floor(position.Y)));
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var target = nodes[(i + 1 + _random.Next(nodes.Count - 1)) % nodes.Count];
            DigRiver(comp, nodes[i], target);
        }

        foreach (var tile in _riverTiles)
        {
            ReserveTile(ent, tile);

            var coords = new EntityCoordinates(ent.Owner, new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
            _pendingSpawns.Add((riverEntity, coords));

            if (comp.RiverBridge is { } bridge && _random.Prob(comp.RiverBridgeChance))
                _pendingSpawns.Add((bridge, coords));
        }

        foreach (var tile in _shoreTiles)
        {
            if (!_riverTiles.Contains(tile))
                ReserveTile(ent, tile);
        }
    }

    private void DigRiver(ADTLavalandGenerationComponent comp, Vector2i start, Vector2i target)
    {
        var current = start;
        var direction = DirectionIndex(current, target);
        var detouring = false;
        var maxSteps = (Math.Abs(target.X - start.X) + Math.Abs(target.Y - start.Y)) * 4;

        if (!IsRiverBlocked(comp, current))
            _riverTiles.Add(current);

        for (var step = 0; step < maxSteps && current != target; step++)
        {
            if (detouring)
            {
                if (_random.Prob(comp.RiverDetourChance))
                {
                    detouring = false;
                    direction = DirectionIndex(current, target);
                }
            }
            else if (_random.Prob(comp.RiverDetourChance))
            {
                detouring = true;
                direction = (direction + (_random.Prob(0.5f) ? 1 : 7)) % RiverDirections.Length;
            }
            else
            {
                direction = DirectionIndex(current, target);
            }

            current += RiverDirections[direction];

            if (IsRiverBlocked(comp, current))
            {
                detouring = false;
                direction = DirectionIndex(current, target);
                current += RiverDirections[direction];
                continue;
            }

            _riverTiles.Add(current);
            SpreadRiver(comp, current, comp.RiverSpreadChance);
        }
    }

    private void SpreadRiver(ADTLavalandGenerationComponent comp, Vector2i origin, float chance)
    {
        if (chance <= 0f)
            return;

        for (var i = 0; i < RiverDirections.Length; i++)
        {
            var tile = origin + RiverDirections[i];

            if (IsRiverBlocked(comp, tile))
                continue;

            var cardinal = i % 2 == 0;

            if (cardinal)
            {
                if (_riverTiles.Add(tile) && _random.Prob(chance))
                    SpreadRiver(comp, tile, chance - comp.RiverSpreadLoss);

                continue;
            }

            if (!_shoreTiles.Contains(tile) && !_riverTiles.Contains(tile) && _random.Prob(chance))
            {
                _riverTiles.Add(tile);
                SpreadRiver(comp, tile, chance - comp.RiverSpreadLoss);
                continue;
            }

            if (!_riverTiles.Contains(tile))
                _shoreTiles.Add(tile);
        }
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

    private static int DirectionIndex(Vector2i from, Vector2i to)
    {
        var step = new Vector2i(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

        for (var i = 0; i < RiverDirections.Length; i++)
        {
            if (RiverDirections[i] == step)
                return i;
        }

        return 0;
    }

    private void ReserveTile(EntityUid map, Vector2i tile)
    {
        _reservedTiles.Clear();

        var bounds = new Box2(tile.X + 0.1f, tile.Y + 0.1f, tile.X + 0.9f, tile.Y + 0.9f);
        _biome.ReserveTiles(map, bounds, _reservedTiles);
    }
}
