using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcNavMemorySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.25);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NPCSteeringComponent, ComponentStartup>(OnSteeringStartup);
    }

    private void OnSteeringStartup(Entity<NPCSteeringComponent> ent, ref ComponentStartup args)
    {
        ResetProgress(EnsureComp<ADTNpcNavMemoryComponent>(ent), _timing.CurTime);
    }

    public static Vector2i GetTile(PathPoly poly)
    {
        return new Vector2i((int) MathF.Floor(poly.Box.Center.X), (int) MathF.Floor(poly.Box.Center.Y));
    }

    public void BlockTile(EntityUid npc, EntityUid obstacle)
    {
        if (!TryComp<ADTNpcNavMemoryComponent>(npc, out var memory))
            return;

        var xform = Transform(obstacle);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        memory.Blocked[(grid, _map.TileIndicesFor(grid, gridComp, xform.Coordinates))] = _timing.CurTime + memory.DoorBlockTime;
    }

    public HashSet<(EntityUid Grid, Vector2i Tile)>? GetBlockedTiles(EntityUid npc)
    {
        if (!TryComp<ADTNpcNavMemoryComponent>(npc, out var memory) || memory.Blocked.Count == 0)
            return null;

        var now = _timing.CurTime;
        HashSet<(EntityUid Grid, Vector2i Tile)>? result = null;
        List<(EntityUid Grid, Vector2i Tile)>? expired = null;

        foreach (var (key, until) in memory.Blocked)
        {
            if (until <= now)
            {
                expired ??= new();
                expired.Add(key);
                continue;
            }

            result ??= new();
            result.Add(key);
        }

        if (expired != null)
        {
            foreach (var key in expired)
            {
                memory.Blocked.Remove(key);
            }
        }

        return result;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NPCSteeringComponent, ADTNpcNavMemoryComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var steering, out var memory, out var xform))
        {
            if (now < memory.NextCheck)
                continue;

            memory.NextCheck = now + CheckInterval;

            if (!memory.Target.Equals(steering.Coordinates))
            {
                ResetProgress(memory, now);
                memory.Target = steering.Coordinates;
            }

            if (steering.Status != SteeringStatus.Moving ||
                steering.DoAfterId != null ||
                steering.Pathfind ||
                !IsStaticTarget(steering.Coordinates))
            {
                memory.LastProgress = now;
                continue;
            }

            if (GetRemaining(uid, steering, xform) is not { } remaining)
                continue;

            var end = steering.CurrentPath.Count > 0 ? steering.CurrentPath.Last() : null;
            if (!ReferenceEquals(end, memory.LastPathEnd))
            {
                memory.LastPathEnd = end;
                memory.BestRemaining = remaining;
                continue;
            }

            if (remaining < memory.BestRemaining - memory.ProgressDistance)
            {
                memory.BestRemaining = remaining;
                memory.LastProgress = now;
                continue;
            }

            if (now - memory.LastProgress < memory.ProgressTimeout)
                continue;

            memory.StuckCount++;
            memory.LastProgress = now;
            memory.BestRemaining = float.MaxValue;

            if (memory.StuckCount <= memory.MaxStuckRepaths && TryBlockAhead(steering, xform, memory, now))
            {
                steering.CurrentPath.Clear();
                memory.LastPathEnd = null;
                continue;
            }

            steering.Status = SteeringStatus.NoPath;
        }
    }

    private void ResetProgress(ADTNpcNavMemoryComponent memory, TimeSpan now)
    {
        memory.Target = default;
        memory.LastPathEnd = null;
        memory.BestRemaining = float.MaxValue;
        memory.LastProgress = now;
        memory.StuckCount = 0;
    }

    private bool IsStaticTarget(EntityCoordinates coordinates)
    {
        return HasComp<MapGridComponent>(coordinates.EntityId) || HasComp<MapComponent>(coordinates.EntityId);
    }

    private float? GetRemaining(EntityUid uid, NPCSteeringComponent steering, TransformComponent xform)
    {
        var ours = _transform.GetMapCoordinates(uid, xform);
        var destination = _transform.ToMapCoordinates(steering.Coordinates);
        if (ours.MapId != destination.MapId)
            return null;

        var total = 0f;
        var previous = ours.Position;

        if (!steering.DirectMove)
        {
            foreach (var node in steering.CurrentPath)
            {
                if (!Exists(node.GraphUid))
                    return null;

                var position = _transform.ToMapCoordinates(node.Coordinates).Position;
                total += (position - previous).Length();
                previous = position;
            }
        }

        return total + (destination.Position - previous).Length();
    }

    private bool TryBlockAhead(NPCSteeringComponent steering, TransformComponent xform, ADTNpcNavMemoryComponent memory, TimeSpan now)
    {
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var ourTile = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var destinationTile = _map.TileIndicesFor(grid, gridComp, steering.Coordinates);

        foreach (var node in steering.CurrentPath)
        {
            if (node.GraphUid != grid)
                return false;

            var tile = GetTile(node);
            if (tile == ourTile)
                continue;

            if (tile == destinationTile)
                return false;

            memory.Blocked[(grid, tile)] = now + memory.StuckBlockTime;
            return true;
        }

        return false;
    }
}
