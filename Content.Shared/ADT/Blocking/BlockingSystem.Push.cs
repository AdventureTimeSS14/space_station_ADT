using System.Numerics;
using Content.Shared.Blocking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    [Dependency] private ThrowingSystem _throwing = default!;

    private const float ShieldPushSpeed = 6f;

    private readonly HashSet<Entity<MobStateComponent>> _shieldPushCandidates = new();

    private bool TryPrepareShieldPush(EntityUid user,
        TransformComponent xform,
        BlockingComponent blocking,
        out EntityCoordinates pushTarget,
        out List<EntityUid> pushed)
    {
        pushTarget = default;
        pushed = new List<EntityUid>();

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var userTile = _transformSystem.GetGridOrMapTilePosition(user, xform);
        var center = TileCenter(gridUid, grid, userTile);

        _shieldPushCandidates.Clear();
        _lookup.GetEntitiesInRange(center, blocking.Shape.Radius, _shieldPushCandidates);

        foreach (var mob in _shieldPushCandidates)
        {
            if (mob.Owner == user)
                continue;

            if (!TryComp<PhysicsComponent>(mob, out var physics))
                continue;

            if (!physics.CanCollide || !physics.Hard)
                continue;

            if ((physics.CollisionMask & (int)CollisionGroup.WallLayer) == 0)
                continue;

            pushed.Add(mob);
        }

        if (pushed.Count == 0)
            return true;

        var forward = xform.LocalRotation.GetCardinalDir();
        var right = forward.GetClockwise90Degrees();

        var order = new[]
        {
            forward,
            right,
            right.GetOpposite(),
            forward.GetOpposite(),
        };

        foreach (var dir in order)
        {
            var tile = userTile + dir.ToIntVec();

            if (!IsShieldPushTileFree(gridUid, grid, tile))
                continue;

            pushTarget = TileCenter(gridUid, grid, tile);
            return true;
        }

        return false;
    }

    private void PushFromShield(EntityUid user, List<EntityUid> pushed, EntityCoordinates target)
    {
        var targetPos = _transformSystem.ToMapCoordinates(target).Position;

        foreach (var mob in pushed)
        {
            var direction = targetPos - _transformSystem.GetWorldPosition(mob);
            if (direction.LengthSquared() <= 0f)
                continue;

            _throwing.TryThrow(mob,
                direction,
                ShieldPushSpeed,
                user,
                pushbackRatio: 0f,
                compensateFriction: true,
                recoil: false,
                playSound: false,
                doSpin: false);
        }
    }

    private bool IsShieldPushTileFree(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var tileRef = _turf.GetTileRef(TileCenter(gridUid, grid, tile));
        if (tileRef is not { } turf || turf.Tile.IsEmpty)
            return false;

        return !_turf.IsTileBlocked(gridUid, tile, CollisionGroup.MobMask, grid);
    }

    private static EntityCoordinates TileCenter(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var size = grid.TileSize;
        return new EntityCoordinates(gridUid, new Vector2(tile.X * size + size / 2f, tile.Y * size + size / 2f));
    }
}
