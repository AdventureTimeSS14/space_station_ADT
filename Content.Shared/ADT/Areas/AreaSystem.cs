using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<AreaGridComponent> _areaGridQuery;
    private EntityQuery<AreaComponent> _areaQuery;

    public override void Initialize()
    {
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _areaGridQuery = GetEntityQuery<AreaGridComponent>();
        _areaQuery = GetEntityQuery<AreaComponent>();
    }

    public bool TryGetArea(EntityCoordinates coordinates, [NotNullWhen(true)] out EntProtoId<AreaComponent>? area)
    {
        area = null;

        if (_transform.GetGrid(coordinates) is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var grid))
        {
            return false;
        }

        var indices = _map.CoordinatesToTile(gridId, grid, coordinates);

        if (_areaGridQuery.TryComp(gridId, out var areaGrid) &&
            areaGrid.Areas.TryGetValue(indices, out var bakedArea))
        {
            area = bakedArea;
            return true;
        }

        foreach (var anchored in _map.GetAnchoredEntities(gridId, grid, indices))
        {
            if (!_areaQuery.HasComp(anchored) ||
                !TryComp<MetaDataComponent>(anchored, out var metaData) ||
                metaData.EntityPrototype is not { } prototype)
            {
                continue;
            }

            area = new EntProtoId<AreaComponent>(prototype.ID);
            return true;
        }

        return false;
    }

    public EntProtoId<AreaComponent>? GetAreaPrototypeId(EntityCoordinates coordinates)
    {
        return TryGetArea(coordinates, out var area) ? area : null;
    }

    public bool TryGetAreaCenter(EntProtoId<AreaComponent> area, EntityUid gridUid, out EntityCoordinates center)
    {
        center = default;

        if (!_mapGridQuery.TryComp(gridUid, out var grid))
            return false;

        var found = false;
        var min = default(Vector2i);
        var max = default(Vector2i);

        void UpdateBounds(Vector2i indices)
        {
            if (!found)
            {
                min = indices;
                max = indices;
                found = true;
                return;
            }

            min = Vector2i.ComponentMin(min, indices);
            max = Vector2i.ComponentMax(max, indices);
        }

        if (_areaGridQuery.TryComp(gridUid, out var areaGrid))
        {
            foreach (var (indices, proto) in areaGrid.Areas)
            {
                if (proto == area)
                    UpdateBounds(indices);
            }
        }

        if (!found)
        {
            var query = AllEntityQuery<AreaComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out _, out var xform))
            {
                if (xform.GridUid != gridUid ||
                    !TryComp<MetaDataComponent>(uid, out var metaData) ||
                    metaData.EntityPrototype?.ID != area.Id)
                {
                    continue;
                }

                var indices = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                UpdateBounds(indices);
            }
        }

        if (!found)
            return false;

        center = _map.ToCoordinates(gridUid, (min + max) / 2, grid);
        return true;
    }
}
