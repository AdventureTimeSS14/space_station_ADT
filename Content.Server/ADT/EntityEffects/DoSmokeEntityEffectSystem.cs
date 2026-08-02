using Content.Shared.ADT.EntityEffects;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.EntityEffects;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class DoSmokeEntityEffectSystem : EntityEffectSystem<TransformComponent, DoSmokeEntityEffect>
{
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<DoSmokeEntityEffect> args)
    {
        var uid = entity.Owner;
        var xform = entity.Comp;
        var effect = args.Effect;

        var mapCoords = _xform.GetMapCoordinates(uid, xform);

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
            return;

        // Получаем MapSystem
        var mapSystem = EntityManager.System<MapSystem>();
        var tileRef = mapSystem.GetTileRef(gridUid, grid, xform.Coordinates);
        if (tileRef.Tile.IsEmpty)
            return;

        // Проверяем необходимость пола
        if (_spreader.RequiresFloorToSpread(new EntProtoId<EdgeSpreaderComponent>(effect.SmokePrototype)) && tileRef.Tile.IsEmpty)
            return;

        var coords = mapSystem.MapToGrid(gridUid, mapCoords);
        var ent = Spawn(effect.SmokePrototype, coords.SnapToGrid());
        
        if (!TryComp<SmokeComponent>(ent, out var smoke))
        {
            QueueDel(ent);
            return;
        }

        _smoke.StartSmoke(ent, effect.Solution, effect.Duration, effect.SpreadAmount, smoke);
    }
}