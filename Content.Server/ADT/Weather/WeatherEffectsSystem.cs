using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Content.Shared.Whitelist;
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
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<WeatherStatusEffectComponent> _weatherQuery;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
        _weatherQuery = GetEntityQuery<WeatherStatusEffectComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = _weatherQuery.GetEnumerator();
        while (query.MoveNext(out var uid, out var weatherComp))
        {
            if (now < weatherComp.NextUpdate)
                continue;

            weatherComp.NextUpdate = now + weatherComp.UpdateDelay;
            Dirty(uid, weatherComp);

            var status = MetaData(uid);
            if (!_statusEffects.TryGetStatusEffectComponent<WeatherStatusEffectComponent>(uid, out var statusComp))
                continue;

            var weatherProto = _proto.TryIndex<WeatherPrototype>(statusComp.AppliedProtoId, out var weather) ? weather : null;
            if (weather?.Damage is not {} damage)
                continue;

            // start and end do no damage
            var percent = _weather.GetWeatherPercent((uid, statusComp));
            if (percent < 1f)
                continue;

            var mapEnt = statusComp.AppliedTo ?? uid;
            UpdateDamage(mapEnt, damage);
        }
    }

    private void UpdateDamage(EntityUid map, DamageSpecifier damage)
    {
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var xform))
        {
            // don't give dead bodies 10000 burn, that's not fun for anyone
            if (xform.MapUid != map || mob.CurrentState == MobState.Dead)
                continue;

            // if not in space, check for being indoors
            if (xform.GridUid is {} gridUid && _gridQuery.TryComp(gridUid, out var grid))
            {
                var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
                if (!_weather.CanWeatherAffect(gridUid, grid, tile, null))
                    continue;
            }

            // Check if there's any active weather on this map
            var weatherQuery = _weatherQuery.GetEnumerator();
            while (weatherQuery.MoveNext(out var wUid, out var _))
            {
                if (!_statusEffects.TryGetStatusEffectComponent<WeatherStatusEffectComponent>(wUid, out var wStatusComp))
                    continue;

                if (wStatusComp.AppliedTo != map)
                    continue;

                var weatherProto = _proto.TryIndex<WeatherPrototype>(wStatusComp.AppliedProtoId, out var weather) ? weather : null;
                if (weather == null)
                    continue;

                if (!_whitelist.IsWhitelistPassOrNull(weather.DamageBlacklist, uid))
                    continue;

                _damageable.TryChangeDamage(uid, damage, interruptsDoAfters: false);
            }
        }
    }
}
