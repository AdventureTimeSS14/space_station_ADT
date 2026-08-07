using System.Numerics;
using Content.Shared.ADT.AnimatedTiles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.AnimatedTiles;

/// <summary>
/// Draws animated frames over tiles whose type has an <see cref="AnimatedTilePrototype"/>
/// </summary>
public sealed class AnimatedTileOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private readonly Dictionary<int, AnimatedTilePrototype> _animated = new();

    private readonly Dictionary<int, Texture> _frames = new();

    private List<Entity<MapGridComponent>> _grids = new();

    public AnimatedTileOverlay(SpriteSystem sprite, SharedMapSystem map, SharedTransformSystem transform)
    {
        _sprite = sprite;
        _map = map;
        _transform = transform;
        IoCManager.InjectDependencies(this);

        BuildRegistry();
    }

    public void BuildRegistry()
    {
        _animated.Clear();

        foreach (var proto in _protoManager.EnumeratePrototypes<AnimatedTilePrototype>())
        {
            if (!_tileDefManager.TryGetDefinition(proto.ID, out var definition))
            {
                Logger.GetSawmill("animated-tiles")
                    .Warning($"Animated tile '{proto.ID}' does not match any tile prototype, ignoring it.");
                continue;
            }

            _animated[definition.TileId] = proto;
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _animated.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var worldAABB = args.WorldAABB;
        var curTime = _timing.RealTime;

        _frames.Clear();

        foreach (var (tileId, proto) in _animated)
        {
            _frames[tileId] = _sprite.GetFrame(proto.Sprite, curTime);
        }

        _grids.Clear();
        _mapManager.FindGridsIntersecting(args.MapId, worldAABB, ref _grids);

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var grid in _grids)
        {
            var tileSize = grid.Comp.TileSize;
            var matrix = _transform.GetWorldMatrix(grid, xformQuery);
            handle.SetTransform(matrix);

            foreach (var tile in _map.GetTilesIntersecting(grid.Owner, grid.Comp, worldAABB))
            {
                if (!_frames.TryGetValue(tile.Tile.TypeId, out var texture))
                    continue;

                var proto = _animated[tile.Tile.TypeId];
                var bottomLeft = tile.GridIndices * tileSize;
                var box = new Box2(bottomLeft, bottomLeft + new Vector2(tileSize, tileSize));

                handle.DrawTextureRect(texture, box, proto.Color);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
