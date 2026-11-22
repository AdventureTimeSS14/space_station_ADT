using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.ADT.AnimatedTiles;

/// <summary>
/// Draws animated frames over tiles whose type has an <see cref="Content.Shared.ADT.AnimatedTiles.AnimatedTilePrototype"/>
/// </summary>
public sealed class AnimatedTileOverlay : Overlay
{
    [Dependency] private IGameTiming _timing = default!;

    private readonly AnimatedTileSystem _system;
    private readonly SpriteSystem _sprite;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private readonly Dictionary<int, Texture> _frames = new();

    private List<Entity<MapGridComponent>> _grids = new();

    public AnimatedTileOverlay(AnimatedTileSystem system, SpriteSystem sprite, SharedMapSystem map, SharedTransformSystem transform)
    {
        _system = system;
        _sprite = sprite;
        _map = map;
        _transform = transform;
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _system.GridTiles.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var worldAABB = args.WorldAABB;

        _grids.Clear();
        _map.FindGridsIntersecting(args.MapId, worldAABB, ref _grids);

        if (_grids.Count == 0)
            return;

        var curTime = _timing.RealTime;
        _frames.Clear();

        foreach (var grid in _grids)
        {
            if (!_system.GridTiles.TryGetValue(grid.Owner, out var tiles))
                continue;

            var tileSize = grid.Comp.TileSize;
            var (_, _, matrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(grid.Owner);
            var localAABB = invMatrix.TransformBox(worldAABB);
            var drawn = false;

            foreach (var (indices, typeId) in tiles)
            {
                var bottomLeft = indices * tileSize;
                var box = new Box2(bottomLeft, bottomLeft + new Vector2(tileSize, tileSize));

                if (!localAABB.Intersects(box))
                    continue;

                if (!_system.Animated.TryGetValue(typeId, out var proto))
                    continue;

                if (!_frames.TryGetValue(typeId, out var texture))
                {
                    texture = _sprite.GetFrame(proto.Sprite, curTime);
                    _frames[typeId] = texture;
                }

                if (!drawn)
                {
                    handle.SetTransform(matrix);
                    drawn = true;
                }

                handle.DrawTextureRect(texture, box, proto.Color);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
