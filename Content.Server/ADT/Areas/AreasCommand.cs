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

        var gridQuery = GetEntityQuery<MapGridComponent>();
        var query = EntityManager.AllEntityQueryEnumerator<AreaComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var metaData, out var xform))
        {
            if (xform.GridUid is not { } gridId || !gridQuery.TryComp(gridId, out var grid))
                continue;

            if (metaData.EntityPrototype is not { } prototype)
            {
                ctx.WriteLine($"{EntityManager.ToPrettyString(uid)} did not have a prototype.");
                continue;
            }

            var areaGrid = EnsureComp<AreaGridComponent>(gridId);
            var indices = _map.CoordinatesToTile(gridId, grid, xform.Coordinates);
            areaGrid.Areas[indices] = prototype.ID;
            EntityManager.Dirty(gridId, areaGrid);
            QDel(uid);
        }
    }

    [CommandImplementation("load")]
    public void Load()
    {
        _map ??= GetSys<SharedMapSystem>();

        var areaQuery = GetEntityQuery<AreaComponent>();
        var query = EntityManager.AllEntityQueryEnumerator<AreaGridComponent, MapGridComponent>();
        while (query.MoveNext(out var gridId, out var areaGrid, out var grid))
        {
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
            }
        }
    }
}
