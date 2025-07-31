using System.Numerics;
using Content.Shared.ADT.Weather;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Weather;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Weather;

public sealed partial class HeavyWindVisualsSystem : VirtualController
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private TimeSpan _lastEffect = TimeSpan.Zero;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<HeavyWindComponent> _windQuery;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _windQuery = GetEntityQuery<HeavyWindComponent>();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_player.LocalEntity.HasValue)
            return;
        if (!_windQuery.TryComp(Transform(_player.LocalEntity.Value).MapUid, out var wind))
            return;
        if (_timing.CurTime < _lastEffect + TimeSpan.FromSeconds(wind.Speed * 3))
            return;
        CreateEffect(wind);
    }
    private void CreateEffect(HeavyWindComponent comp)
    {
        if (!_player.LocalEntity.HasValue)
            return;

        _lastEffect = _timing.CurTime;
        var uid = _player.LocalEntity.Value;
        var map = _transform.GetMap(uid);
        var xform = Transform(uid);
        if (!map.HasValue || map.Value != comp.Owner)
            return;

        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var gridComp))
            return;

        var tile = _map.GetTileRef((gridUid, gridComp), xform.Coordinates);
        if (!_weather.CanWeatherAffect(gridUid, gridComp, tile))
            return;

        var coords = xform.Coordinates;
        var effectCoords = new EntityCoordinates(coords.EntityId, coords.X + _random.NextFloat(-3f, 3f), coords.Y + _random.NextFloat(-3f, 3f));
        var effect = Spawn("ADTWindEffect", effectCoords);
        _transform.SetLocalRotation(effect, comp.Direction.ToAngle() - Angle.FromDegrees(90));
    }
}
