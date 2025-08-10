using Content.Shared.ADT.Weather;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Weather;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Weather;

public sealed partial class HeavyWindSystem : VirtualController
{
    private EntityQuery<MapGridComponent> _gridQuery;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<HeavyWindComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, HeavyWindComponent comp, ComponentStartup args)
    {
        comp.Direction = _random.NextVector2();
        comp.Speed = _random.NextFloat(2f, 4f);
        Dirty(uid, comp);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        var query = EntityQueryEnumerator<HeavyWindComponent>();
        while (query.MoveNext(out var map, out var wind))
        {
            Move(map, wind, prediction, frameTime);
        }
    }

    private void Move(EntityUid map, HeavyWindComponent wind, bool prediction, float frameTime)
    {
        var entQuery = EntityQueryEnumerator<PullableComponent, PhysicsComponent, TransformComponent>();
        while (entQuery.MoveNext(out var uid, out var pullable, out var physics, out var xform))
        {
            if (xform.MapUid != map)
                continue;
            if (!physics.Predict && prediction)
                continue;
            if (physics.BodyType == BodyType.Static)
                continue;
            if (_container.IsEntityInContainer(uid))
                continue;
            if (xform.GridUid is { } gridUid && _gridQuery.TryComp(gridUid, out var grid))
            {
                var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
                if (!_weather.CanWeatherAffect(gridUid, grid, tile))
                    continue;
            }

            var localPos = xform.LocalPosition;
            localPos += wind.Direction * wind.Speed * frameTime;
            _transform.SetLocalPosition(uid, localPos);
            _physics.SetAwake((uid, physics), true);
            _physics.SetSleepTime(physics, 0f);
        }
    }
}
