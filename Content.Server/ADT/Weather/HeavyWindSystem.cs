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

    private readonly HashSet<EntityUid> _pullableCache = new();

    private readonly List<EntityUid> _toRemove = new();
    private float _windUpdateTimer;
    private const float WindUpdateInterval = 0.25f;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<HeavyWindComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PullableComponent, ComponentInit>(OnPullableInit);
        SubscribeLocalEvent<PullableComponent, ComponentShutdown>(OnPullableShutdown);
    }

    private void OnPullableInit(EntityUid uid, PullableComponent component, ComponentInit args)
    {
        _pullableCache.Add(uid);
    }

    private void OnPullableShutdown(EntityUid uid, PullableComponent component, ComponentShutdown args)
    {
        _pullableCache.Remove(uid);
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

        _windUpdateTimer += frameTime;
        if (_windUpdateTimer < WindUpdateInterval)
            return;
        _windUpdateTimer -= WindUpdateInterval;

        var query = EntityQueryEnumerator<HeavyWindComponent>();
        while (query.MoveNext(out var map, out var wind))
        {
            Move(map, wind, prediction, frameTime);
        }
    }

    private void Move(EntityUid map, HeavyWindComponent wind, bool prediction, float frameTime)
    {
        _toRemove.Clear();
        foreach (var uid in _pullableCache)
        {
            if (!Exists(uid) ||
                !TryComp<PullableComponent>(uid, out var pullable) ||
                !TryComp<PhysicsComponent>(uid, out var physics) ||
                !TryComp<TransformComponent>(uid, out var xform))
            {
                _toRemove.Add(uid);
                continue;
            }

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

        // Clean up removed entities
        foreach (var uid in _toRemove)
        {
            _pullableCache.Remove(uid);
        }
    }
}
