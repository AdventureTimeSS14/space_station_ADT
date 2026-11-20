using Content.Server.Administration;
using Content.Shared.ADT.Areas;
using Content.Shared.Administration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server.ADT.Areas;

[ToolshedCommand, AdminCommand(AdminFlags.Mapping)]
public sealed class AreasCommand : ToolshedCommand
{
    private SharedMapSystem? _map;

    [CommandImplementation("save")]
    public void Save([CommandInvocationContext] IInvocationContext ctx)
    {
        _map ??= GetSys<SharedMapSystem>();

        if (!TryGetUserMap(ctx, out var mapId))
            return;

        var gridQuery = GetEntityQuery<MapGridComponent>();
        var processed = new HashSet<EntityUid>();
        var pending = new List<(EntityUid Uid, MetaDataComponent MetaData, TransformComponent Xform)>();
        var saved = 0;

        while (true)
        {
            pending.Clear();
            var query = EntityManager.AllEntityQueryEnumerator<AreaComponent, MetaDataComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out _, out var metaData, out var xform))
            {
                if (xform.MapID == mapId && processed.Add(uid))
                    pending.Add((uid, metaData, xform));
            }

            if (pending.Count == 0)
                break;

            foreach (var (uid, metaData, xform) in pending)
            {
                if (xform.GridUid is not { } gridId || !gridQuery.TryComp(gridId, out var grid))
                {
                    ctx.WriteLine($"{EntityManager.ToPrettyString(uid)} is not on a grid and was deleted.");
                    Del(uid);
                    continue;
                }

                if (metaData.EntityPrototype is not { } prototype)
                {
                    ctx.WriteLine($"{EntityManager.ToPrettyString(uid)} did not have a prototype and was deleted.");
                    Del(uid);
                    continue;
                }

                var areaGrid = EnsureComp<AreaGridComponent>(gridId);
                var indices = _map.CoordinatesToTile(gridId, grid, xform.Coordinates);
                areaGrid.Areas[indices] = prototype.ID;
                EntityManager.Dirty(gridId, areaGrid);
                Del(uid);
                saved++;
            }
        }

        var left = 0;
        var leftQuery = EntityManager.AllEntityQueryEnumerator<AreaComponent, TransformComponent>();
        while (leftQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != mapId || EntityManager.IsQueuedForDeletion(uid) || Deleted(uid))
                continue;

            ctx.WriteLine($"{EntityManager.ToPrettyString(uid)} was not removed.");
            left++;
        }

        ctx.WriteLine($"Saved {saved} area tiles on {mapId}, {left} area entities left.");
    }

    [CommandImplementation("load")]
    public void Load([CommandInvocationContext] IInvocationContext ctx)
    {
        _map ??= GetSys<SharedMapSystem>();

        if (!TryGetUserMap(ctx, out var mapId))
            return;

        var areaQuery = GetEntityQuery<AreaComponent>();
        var spawned = 0;
        var query = EntityManager.AllEntityQueryEnumerator<AreaGridComponent, MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var gridId, out var areaGrid, out var grid, out var gridXform))
        {
            if (gridXform.MapID != mapId)
                continue;

            foreach (var (indices, areaProto) in areaGrid.Areas)
            {
                var alreadySpawned = false;
                foreach (var anchored in _map.GetAnchoredEntities(gridId, grid, indices))
                {
                    if (areaQuery.HasComp(anchored))
                    {
                        alreadySpawned = true;
                        break;
                    }
                }

                if (alreadySpawned)
                    continue;

                var coordinates = _map.ToCoordinates(gridId, indices, grid);
                Spawn(areaProto, coordinates);
                spawned++;
            }
        }

        ctx.WriteLine($"Spawned {spawned} area entities on {mapId}.");
    }

    private bool TryGetUserMap(IInvocationContext ctx, out MapId mapId)
    {
        mapId = MapId.Nullspace;

        if (ctx.Session?.AttachedEntity is not { } user)
        {
            ctx.WriteLine("You need to be attached to an entity on the map you want to edit.");
            return false;
        }

        mapId = Transform(user).MapID;
        if (mapId != MapId.Nullspace)
            return true;

        ctx.WriteLine("You are not on a map.");
        return false;
    }
}
