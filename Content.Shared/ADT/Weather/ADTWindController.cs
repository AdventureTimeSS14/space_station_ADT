using System.Numerics;
using Content.Shared.ADT.Weather.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weather;

public sealed class ADTWindController : VirtualController
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;

    private readonly Dictionary<EntityUid, (Entity<ADTWeatherWindComponent> Wind, Vector2 Velocity)> _winds = new();

    private readonly FastNoiseLite _noise = new();

    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(SharedMoverController));

        base.Initialize();

        _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        if (!CollectWinds())
            return;

        var query = EntityQueryEnumerator<MobStateComponent, PhysicsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var mob, out var physics, out var xform))
        {
            if (xform.MapUid is not { } map || !_winds.TryGetValue(map, out var wind))
                continue;

            if (mob.CurrentState == MobState.Dead)
                continue;

            if (prediction && !physics.Predict)
                continue;

            if (physics.BodyStatus != BodyStatus.OnGround)
                continue;

            if (_whitelist.IsWhitelistFail(wind.Wind.Comp.Whitelist, uid))
                continue;

            if (!IsExposed(xform))
                continue;

            var velocity = physics.LinearVelocity;

            SharedMoverController.Accelerate(ref velocity, wind.Velocity, wind.Wind.Comp.Acceleration, frameTime);

            PhysicsSystem.SetLinearVelocity(uid, velocity, body: physics);
        }
    }

    private bool CollectWinds()
    {
        _winds.Clear();

        var query = EntityQueryEnumerator<ADTWeatherWindComponent, StatusEffectComponent>();

        while (query.MoveNext(out var uid, out var wind, out var status))
        {
            if (status.AppliedTo is not { } map)
                continue;

            var percent = _weather.GetWeatherPercent((uid, status));
            if (percent <= 0f)
                continue;

            _winds[map] = ((uid, wind), GetWindVelocity(wind, percent));
        }

        return _winds.Count > 0;
    }

    private Vector2 GetWindVelocity(ADTWeatherWindComponent wind, float percent)
    {
        var time = (float) _timing.CurTime.TotalSeconds;

        var offset = wind.Seed * 128f;
        var swing = _noise.GetNoise(time * wind.DirectionFrequency, offset);
        var gust = _noise.GetNoise(time * wind.GustFrequency, offset + 64f);

        var direction = new Angle(wind.BaseDirection.Theta + wind.Spread.Theta * swing);
        var speed = wind.Speed * (1f + wind.GustAmplitude * gust) * percent;

        if (speed <= 0f)
            return Vector2.Zero;

        return direction.ToVec() * speed;
    }

    private bool IsExposed(TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return true;

        var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
        return _weather.CanWeatherAffect((gridUid, (MapGridComponent?) grid, null), tile);
    }
}
