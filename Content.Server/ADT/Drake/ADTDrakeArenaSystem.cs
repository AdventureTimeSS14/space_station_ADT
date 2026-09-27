using System.Linq;
using Content.Server.Destructible;
using Content.Shared.ADT.Drake;
using Content.Shared.Chasm;
using Content.Shared.Mech.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeArenaSystem : EntitySystem
{
    [Dependency] private readonly ADTDrakeEffectsSystem _effects = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly Direction[] Cardinals =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    private const int DenseLayer = (int) (CollisionGroup.Opaque | CollisionGroup.Impassable);

    private readonly HashSet<EntityUid> _roundBuffer = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDrakeArenaComponent, ADTDrakeComponent>();

        while (query.MoveNext(out var uid, out var arena, out var drake))
        {
            if (_mobState.IsDead(uid))
            {
                RemCompDeferred<ADTDrakeArenaComponent>(uid);
                continue;
            }

            if (now < arena.NextRoundAt)
                continue;

            var ent = new Entity<ADTDrakeComponent>(uid, drake);

            if (arena.RoundsLeft <= 0)
            {
                Finish(ent, true);
                continue;
            }

            if (!RunRound(ent, arena))
            {
                foreach (var wall in arena.Walls)
                {
                    QueueDel(wall);
                }

                Finish(ent, false);
                continue;
            }

            arena.RoundsLeft--;
            arena.NextRoundAt = now + drake.ArenaRoundDelay;
        }
    }

    public void StartArena(Entity<ADTDrakeComponent> ent, EntityUid? target)
    {
        if (target is not { } victim || TerminatingOrDeleted(victim))
        {
            Finish(ent, false);
            return;
        }

        var xform = Transform(victim);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Finish(ent, false);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-drake-arena", ("drake", ent.Owner)), victim, PopupType.LargeCaution);

        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var arena = EnsureComp<ADTDrakeArenaComponent>(ent);
        arena.Grid = gridUid;
        arena.Tiles.Clear();
        arena.Indestructible.Clear();
        arena.Walls.Clear();
        arena.RoundsLeft = ent.Comp.ArenaRounds;
        arena.NextRoundAt = _timing.CurTime + ent.Comp.ArenaStartDelay;

        foreach (var tile in Ring(center, ent.Comp.ArenaWallRadius))
        {
            var wall = Spawn(ent.Comp.ArenaWallProto, _map.GridTileToLocal(gridUid, grid, tile));
            _transform.SetLocalRotation(wall, _random.Pick(Cardinals).ToAngle());
            arena.Walls.Add(wall);
        }

        var basalt = new Tile(_tileDefinitions[ent.Comp.ArenaFloorTile].TileId);

        foreach (var tile in Square(center, ent.Comp.ArenaRadius))
        {
            arena.Tiles.Add(tile);

            if (IsIndestructibleWall(gridUid, grid, tile))
            {
                arena.Indestructible.Add(tile);
                continue;
            }

            ClearToBasalt(gridUid, grid, tile, basalt);
        }
    }

    private bool RunRound(Entity<ADTDrakeComponent> ent, ADTDrakeArenaComponent arena)
    {
        if (!TryComp<MapGridComponent>(arena.Grid, out var grid))
            return false;

        var tiles = arena.Tiles.ToHashSet();
        var safe = new HashSet<Vector2i>(arena.Indestructible);
        var anyAttack = false;

        _roundBuffer.Clear();

        foreach (var tile in arena.Tiles)
        {
            foreach (var uid in _lookup.GetLocalEntitiesIntersecting(arena.Grid, tile, gridComp: grid))
            {
                if (!_roundBuffer.Add(uid))
                    continue;

                int ring;
                if (HasComp<MobStateComponent>(uid) && HasComp<ActorComponent>(uid))
                    ring = ent.Comp.ArenaSafeRing;
                else if (HasComp<MechComponent>(uid))
                    ring = ent.Comp.ArenaMechSafeRing;
                else
                    continue;

                anyAttack = true;

                var own = _map.TileIndicesFor(arena.Grid, grid, Transform(uid).Coordinates);
                var candidates = Ring(own, ring)
                    .Where(t => tiles.Contains(t) && !safe.Contains(t))
                    .ToList();

                if (candidates.Count > 0)
                    safe.Add(_random.Pick(candidates));
            }
        }

        if (!anyAttack)
            return false;

        foreach (var tile in arena.Tiles)
        {
            if (!safe.Contains(tile))
            {
                _effects.SpawnLavaWarning(ent.Comp.LavaWarningProto, arena.Grid, grid, tile, ent.Comp.ArenaLavaResetTime);
                continue;
            }

            if (!arena.Indestructible.Contains(tile))
                Spawn(ent.Comp.LavaSafeProto, _map.GridTileToLocal(arena.Grid, grid, tile));
        }

        return true;
    }

    private void Finish(Entity<ADTDrakeComponent> ent, bool success)
    {
        RemCompDeferred<ADTDrakeArenaComponent>(ent);

        var ev = new ADTDrakeArenaFinishedEvent(success);
        RaiseLocalEvent(ent, ref ev);
    }

    private bool IsIndestructibleWall(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (IsWall(anchored) && !HasComp<DestructibleComponent>(anchored))
                return true;
        }

        return false;
    }

    private void ClearToBasalt(EntityUid gridUid, MapGridComponent grid, Vector2i tile, Tile basalt)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
        {
            if (IsWall(anchored) || HasComp<ChasmComponent>(anchored) || IsLava(anchored))
                QueueDel(anchored);
        }

        if (_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) && tileRef.Tile.TypeId == basalt.TypeId)
            return;

        _map.SetTile(gridUid, grid, tile, basalt);
    }

    private bool IsWall(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics) || !physics.CanCollide || !physics.Hard)
            return false;

        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return false;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (fixture.Hard && (fixture.CollisionLayer & DenseLayer) == DenseLayer)
                return true;
        }

        return false;
    }

    private bool IsLava(EntityUid uid)
    {
        var id = MetaData(uid).EntityPrototype?.ID;
        return id is "FloorLavaEntity" or "ADTDrakeTempLava";
    }

    private static IEnumerable<Vector2i> Square(Vector2i center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                yield return center + new Vector2i(x, y);
            }
        }
    }

    private static IEnumerable<Vector2i> Ring(Vector2i center, int radius)
    {
        foreach (var tile in Square(center, radius))
        {
            var delta = tile - center;
            if (Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) == radius)
                yield return tile;
        }
    }
}

[RegisterComponent]
public sealed partial class ADTDrakeArenaComponent : Component
{
    [ViewVariables]
    public EntityUid Grid;

    [ViewVariables]
    public List<Vector2i> Tiles = new();

    [ViewVariables]
    public HashSet<Vector2i> Indestructible = new();

    [ViewVariables]
    public List<EntityUid> Walls = new();

    [ViewVariables]
    public int RoundsLeft;

    [ViewVariables]
    public TimeSpan NextRoundAt;
}

[ByRefEvent]
public readonly record struct ADTDrakeArenaFinishedEvent(bool Success);
