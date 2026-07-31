using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Shuttles.UI;

/// <summary>
/// Cached tile-fill and contour renderer for foreign grids on the crew-monitoring map.
/// Shuttle consoles keep this logic inlined in <c>BaseShuttleControl</c>.
/// </summary>
public sealed class GridRadarRenderer
{
    private readonly SharedMapSystem _maps;
    private readonly IParallelManager _parallel;
    private readonly Dictionary<EntityUid, GridDrawData> _gridData = new();
    private readonly List<Vector2i> _tiles = new();
    private readonly HashSet<Vector2i> _neighbors = new();
    private readonly List<(Vector2 Start, Vector2 End)> _edges = new();
    private readonly (DirectionFlag Direction, Vector2i Offset)[] _directions;
    private GridDrawJob _drawJob;
    private Vector2[] _vertices = [];

    public GridRadarRenderer(SharedMapSystem maps, IParallelManager parallel)
    {
        _maps = maps;
        _parallel = parallel;
        _drawJob = new GridDrawJob { ScaledVertices = _vertices };
        _directions = new (DirectionFlag, Vector2i)[4];

        for (var i = 0; i < _directions.Length; i++)
        {
            var direction = (DirectionFlag) Math.Pow(2, i);
            _directions[i] = (direction, direction.AsDir().ToIntVec());
        }
    }

    public void DrawGrid(
        DrawingHandleScreen handle,
        Matrix3x2 gridToView,
        Entity<MapGridComponent> grid,
        Color color,
        float alpha = 0.01f)
    {
        var data = _gridData.GetOrNew(grid.Owner);
        if (data.LastBuild < grid.Comp.LastTileModifiedTick)
            RebuildGrid(grid, data);

        var total = data.Vertices.Count;
        if (total == 0)
            return;

        Extensions.EnsureLength(ref _vertices, total);
        _drawJob.Matrix = gridToView;
        _drawJob.Vertices = data.Vertices;
        _drawJob.ScaledVertices = _vertices;
        _parallel.ProcessNow(_drawJob, total);

        const int batchSize = 3 * 4096;
        for (var start = 0; start < data.EdgeIndex; start += batchSize)
        {
            var count = Math.Min(batchSize, data.EdgeIndex - start);
            handle.DrawPrimitives(
                DrawPrimitiveTopology.TriangleList,
                new Span<Vector2>(_vertices, start, count),
                color.WithAlpha(alpha));
        }

        var edgeCount = total - data.EdgeIndex;
        if (edgeCount > 0)
        {
            handle.DrawPrimitives(
                DrawPrimitiveTopology.LineList,
                new Span<Vector2>(_vertices, data.EdgeIndex, edgeCount),
                color);
        }
    }

    private void RebuildGrid(Entity<MapGridComponent> grid, GridDrawData data)
    {
        data.Vertices.Clear();
        _tiles.Clear();
        _neighbors.Clear();

        var enumerator = _maps.GetAllTilesEnumerator(grid.Owner, grid.Comp);
        var tileSize = grid.Comp.TileSize;
        while (enumerator.MoveNext(out var tile))
        {
            var index = tile.Value.GridIndices;
            _tiles.Add(index);
            _neighbors.Add(index);

            var bottomLeft = _maps.TileToVector(grid, index);
            var bottomRight = bottomLeft + new Vector2(tileSize, 0f);
            var topRight = bottomLeft + new Vector2(tileSize, tileSize);
            var topLeft = bottomLeft + new Vector2(0f, tileSize);
            data.Vertices.Add(bottomLeft);
            data.Vertices.Add(bottomRight);
            data.Vertices.Add(topLeft);
            data.Vertices.Add(bottomRight);
            data.Vertices.Add(topLeft);
            data.Vertices.Add(topRight);
        }

        data.EdgeIndex = data.Vertices.Count;
        _edges.Clear();
        foreach (var index in _tiles)
        {
            foreach (var (direction, offset) in _directions)
            {
                if (_neighbors.Contains(index + offset))
                    continue;

                var bottomLeft = _maps.TileToVector(grid, index);
                var bottomRight = bottomLeft + new Vector2(tileSize, 0f);
                var topRight = bottomLeft + new Vector2(tileSize, tileSize);
                var topLeft = bottomLeft + new Vector2(0f, tileSize);
                _edges.Add(direction switch
                {
                    DirectionFlag.South => (bottomLeft, bottomRight),
                    DirectionFlag.East => (bottomRight, topRight),
                    DirectionFlag.North => (topRight, topLeft),
                    DirectionFlag.West => (topLeft, bottomLeft),
                    _ => throw new NotImplementedException(),
                });
            }
        }

        MergeCollinearEdges();
        data.Vertices.EnsureCapacity(data.Vertices.Count + _edges.Count * 2);
        foreach (var edge in _edges)
        {
            data.Vertices.Add(edge.Start);
            data.Vertices.Add(edge.End);
        }

        data.LastBuild = grid.Comp.LastTileModifiedTick;
    }

    private void MergeCollinearEdges()
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            for (var i = 0; i < _edges.Count; i++)
            {
                var (start, end) = _edges[i];
                for (var j = i + 1; j < _edges.Count; j++)
                {
                    var (nextStart, nextEnd) = _edges[j];
                    if (!end.Equals(nextStart) ||
                        !CollinearSimplifier.IsCollinear(
                            start,
                            end,
                            nextEnd,
                            10f * float.Epsilon))
                    {
                        continue;
                    }

                    _edges[i] = (start, nextEnd);
                    _edges.RemoveAt(j);
                    changed = true;
                    break;
                }
            }
        }
    }

    private record struct GridDrawJob : IParallelRobustJob
    {
        public int BatchSize => 64;
        public Matrix3x2 Matrix;
        public List<Vector2> Vertices;
        public Vector2[] ScaledVertices;

        public void Execute(int index)
        {
            ScaledVertices[index] = Vector2.Transform(Vertices[index], Matrix);
        }
    }

    private sealed class GridDrawData
    {
        public readonly List<Vector2> Vertices = new();
        public int EdgeIndex;
        public GameTick LastBuild;
    }
}
