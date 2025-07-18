using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared.ADT.Atmos.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.ADT.EntitySystems;

/// <summary>
/// Система, которая будет переносить температуру в атмосфере между стенами.
/// </summary>
public sealed partial class SuperconductionSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private const float MinTempDelta = 0.1f;
    private const float MinBlockerTemp = 1f;

    private readonly List<TempSource> _sources = new();

    private readonly record struct TempSource(
        float Temperature,
        Vector2 WorldPosition,
        MapId MapId,
        float Range
    );

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        Superconduction();
    }

    private void Superconduction()
    {
        _sources.Clear();

        var query = EntityQueryEnumerator<MapGridComponent, TransformComponent, GridAtmosphereComponent>();
        while (query.MoveNext(out var uid, out var grid, out var xform, out var atmos))
        {
            var tiles = atmos.Tiles;
            foreach (var (tilePos, tile) in tiles)
            {
                if (tile.Air == null || tile.Air.Temperature < 300f)
                    continue;

                var tileCoords = new EntityCoordinates(uid, (Vector2)tilePos * grid.TileSize);
                var worldPos = _xform.GetWorldPosition(xform);
                _sources.Add(new TempSource(tile.Air.Temperature, worldPos, xform.MapID, 7f));
            }
        }

        foreach (var src in _sources)
        {
            SpreadTemperature(src);
        }
    }

    private void SpreadTemperature(TempSource source)
    {
        var grids = new List<Entity<MapGridComponent>>();
        _map.FindGridsIntersecting(source.MapId,
            Box2.CenteredAround(source.WorldPosition, new Vector2((source.Range + 1) * 1.5f)),
            (EntityUid uid, MapGridComponent grid) =>
            {
                grids.Add((uid, grid));
                return true;
            });

        foreach (var (gridId, grid) in grids)
        {
            if (!TryComp<GridAtmosphereComponent>(gridId, out var atmos))
                continue;

            var xform = Transform(gridId);
            var gridComp = Comp<MapGridComponent>(gridId);

            var invMatrix = xform.InvWorldMatrix;
            var srcLocal = Vector2.Transform(source.WorldPosition, invMatrix) / gridComp.TileSize;

            for (int dx = -10; dx <= 10; dx++)
                for (int dy = -10; dy <= 10; dy++)
                {
                    var cell = new Vector2i((int)srcLocal.X + dx, (int)srcLocal.Y + dy);
                    if (!atmos.Tiles.TryGetValue(cell, out var tile) || tile.Air == null)
                        continue;

                    var tileCoords = new EntityCoordinates(gridId, (Vector2)cell * gridComp.TileSize);
                    var dstWorld = _xform.GetWorldPosition(xform);
                    var dist = (dstWorld - source.WorldPosition).Length();

                    if (dist > source.Range)
                        continue;

                    var temp = DistanceReduction(source.Temperature, dist);
                    if (temp < MinTempDelta)
                        continue;

                    temp = Raycast((gridId, grid), source.WorldPosition, dstWorld, temp);
                    if (temp < MinTempDelta)
                        continue;

                    var air = tile.Air;
                    if (air != null)
                    {
                        air.Temperature = MathF.Max(air.Temperature, temp);
                    }
                }
        }
    }


    #region Raycast
    /// <summary>
    /// Работа гридкаста. Успешно спиздил всё с губовской рады.
    /// </summary>
    private float Raycast(
        Entity<MapGridComponent> grid,
        Vector2 srcWorld,
        Vector2 dstWorld,
        float temp)
    {
        if (!TryComp<SuperconductiveComponent>(grid, out var res) || res.ResistancePerTile == null)
            return temp;

        var xform = Transform(grid);
        var gridComp = Comp<MapGridComponent>(grid);

        var inv = xform.InvWorldMatrix;
        var srcLocal = Vector2.Transform(srcWorld, inv) / gridComp.TileSize;
        var dstLocal = Vector2.Transform(dstWorld, inv) / gridComp.TileSize;

        foreach (var (cell, dist) in AdvancedGridRaycast(srcLocal, dstLocal))
        {
            if (!res.ResistancePerTile.TryGetValue(cell, out var resistance))
                continue;

            var coeff = resistance > 1f ? 1f / resistance : 1f;
            temp *= MathF.Pow(coeff, dist);

            if (temp < MinBlockerTemp)
                break;
        }

        return temp;
    }

    private static float DistanceReduction(float baseTemp, float distance)
    {
        const float k = 0.7f;
        return baseTemp * MathF.Exp(-k * distance);
    }

    private static IEnumerable<(Vector2i cell, float distInCell)> AdvancedGridRaycast(Vector2 sourceGridPos, Vector2 destGridPos)
    {
        var delta = destGridPos - sourceGridPos;

        var currentX = (int)Math.Floor(sourceGridPos.X);
        var currentY = (int)Math.Floor(sourceGridPos.Y);

        var stepX = 0;
        float tDeltaX = 0, tMaxX = float.MaxValue;
        if (delta.X != 0)
        {
            stepX = delta.X > 0 ? 1 : -1;
            float xEdge = stepX > 0 ? currentX + 1 : currentX;
            tMaxX = (xEdge - sourceGridPos.X) / delta.X;
            tDeltaX = stepX / delta.X;
        }

        var stepY = 0;
        float tDeltaY = 0, tMaxY = float.MaxValue;
        if (delta.Y != 0)
        {
            stepY = delta.Y > 0 ? 1 : -1;
            float yEdge = stepY > 0 ? currentY + 1 : currentY;
            tMaxY = (yEdge - sourceGridPos.Y) / delta.Y;
            tDeltaY = stepY / delta.Y;
        }

        var entry = sourceGridPos;
        while (true)
        {
            var tExit = Math.Min(tMaxX, tMaxY);
            var exitIsX = tMaxX < tMaxY;
            if (tExit > 1f)
                tExit = 1f;

            var exit = sourceGridPos + delta * tExit;
            var cell = new Vector2i(currentX, currentY);
            yield return (cell, (exit - entry).Length());
            if (tExit >= 1f)
                break;

            if (exitIsX)
            {
                currentX += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                currentY += stepY;
                tMaxY += tDeltaY;
            }

            entry = exit;
        }
    }
    #endregion
}