using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Fishing.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Map;

namespace Content.Server.ADT.Fishing;

public sealed class ADTButcherSmokeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTButcherSmokeComponent, ButcherSpawnsModifyEvent>(OnButchered);
    }

    private void OnButchered(Entity<ADTButcherSmokeComponent> ent, ref ButcherSpawnsModifyEvent args)
    {
        var mapCoords = _transform.GetMapCoordinates(ent.Owner);

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
            return;

        var coords = _map.MapToGrid(gridUid, mapCoords);
        var smoke = Spawn(ent.Comp.Prototype, coords.SnapToGrid());

        if (!TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            Del(smoke);
            return;
        }

        _smoke.StartSmoke(smoke, ent.Comp.Solution, ent.Comp.Duration, ent.Comp.SpreadAmount, smokeComp);
    }
}
