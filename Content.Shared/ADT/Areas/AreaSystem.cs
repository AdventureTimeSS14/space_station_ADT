using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<AreaGridComponent> _areaGridQuery;

    public override void Initialize()
    {
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _areaGridQuery = GetEntityQuery<AreaGridComponent>();

        SubscribeLocalEvent<AreaGridComponent, MapInitEvent>(OnAreaGridMapInit);
    }

    private void OnAreaGridMapInit(Entity<AreaGridComponent> ent, ref MapInitEvent args)
    {
        RefreshAreaEntities(ent);
    }

    public void RefreshAreaEntities(Entity<AreaGridComponent> ent)
    {
        if (_net.IsClient)
            return;

        foreach (var areaProto in ent.Comp.Areas.Values.DistinctBy(a => a.Id))
        {
            if (!TryGetAreaCenter(areaProto, ent, out var center))
                continue;

            Spawn(areaProto, center);
        }
    }

    public bool TryGetArea(EntityCoordinates coordinates, [NotNullWhen(true)] out EntProtoId<AreaComponent>? area)
    {
        area = null;

        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var grid) ||
            !_areaGridQuery.TryComp(gridId, out var areaGrid))
        {
            return false;
        }

        var indices = _map.CoordinatesToTile(gridId, grid, coordinates);

        if (!areaGrid.Areas.TryGetValue(indices, out var areaProto))
            return false;

        area = areaProto;
        return true;
    }

    public EntProtoId<AreaComponent>? GetAreaPrototypeId(EntityCoordinates coordinates)
    {
        return TryGetArea(coordinates, out var area) ? area : null;
    }

    public bool TryGetAreaCenter(EntProtoId<AreaComponent> area, EntityUid gridUid, out EntityCoordinates center)
    {
        center = default;

        if (!_mapGridQuery.TryComp(gridUid, out var grid) ||
            !_areaGridQuery.TryComp(gridUid, out var areaGrid))
        {
            return false;
        }

        var found = false;
        var min = default(Vector2i);
        var max = default(Vector2i);

        foreach (var (indices, proto) in areaGrid.Areas)
        {
            if (proto != area)
                continue;

            if (!found)
            {
                min = indices;
                max = indices;
                found = true;
                continue;
            }

            min = Vector2i.ComponentMin(min, indices);
            max = Vector2i.ComponentMax(max, indices);
        }

        if (!found)
            return false;

        center = _map.ToCoordinates(gridUid, (min + max) / 2, grid);
        return true;
    }
}
