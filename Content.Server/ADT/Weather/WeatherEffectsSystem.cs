using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weather;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Weather;

/// <summary>
/// Handles weather damage for exposed entities.
/// </summary>
public sealed partial class WeatherEffectsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;

    private EntityQuery<MapGridComponent> _gridQuery;

    // OPTIMIZATION: Timer to update weather damage periodically instead of every tick
    private float _weatherUpdateTimer;
    private const float WeatherUpdateInterval = 0.5f;

    // OPTIMIZATION: Cache to avoid allocating list every update
    private readonly List<(EntityUid Uid, MobStateComponent Mob, TransformComponent Xform)> _mobCache = new(256);

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // OPTIMIZATION: Only update weather damage periodically - check once per tick, not per-weather
        _weatherUpdateTimer += (float)_timing.TickPeriod.TotalSeconds;
        if (_weatherUpdateTimer < WeatherUpdateInterval)
            return;
        _weatherUpdateTimer -= WeatherUpdateInterval;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WeatherComponent>();
        while (query.MoveNext(out var map, out var weather))
        {
            if (now < weather.NextUpdate)
                continue;

            weather.NextUpdate = now + weather.UpdateDelay;

            foreach (var (id, data) in weather.Weather)
            {
                // start and end do no damage
                if (data.State != WeatherState.Running)
                    continue;

                UpdateDamage(map, id);
            }
        }
    }

    private void UpdateDamage(EntityUid map, ProtoId<WeatherPrototype> id)
    {
        var weather = _proto.Index(id);
        if (weather.Damage is not {} damage)
            return;

        // OPTIMIZATION: Collect mobs into cache first to avoid repeated enumerator overhead
        _mobCache.Clear();
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var xform))
        {
            // Early exit for dead mobs and wrong map
            if (xform.MapUid != map || mob.CurrentState == MobState.Dead)
                continue;

            _mobCache.Add((uid, mob, xform));
        }

        // OPTIMIZATION: Process cached mobs - reduces enumerator overhead
        foreach (var (uid, mob, xform) in _mobCache)
        {
            // if on a grid, check if indoors
            if (xform.GridUid is {} gridUid && _gridQuery.TryComp(gridUid, out var grid))
            {
                var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
                if (!_weather.CanWeatherAffect(gridUid, grid, tile))
                    continue;
            }

            if (_whitelist.IsBlacklistFailOrNull(weather.DamageBlacklist, uid))
                _damageable.TryChangeDamage(uid, damage, interruptsDoAfters: false);
        }
    }
}
