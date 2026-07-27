using Content.Shared.ADT.Weather.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
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
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;

    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var weatherComp, out var statusComp))
        {
            if (now < weatherComp.NextUpdate)
                continue;

            weatherComp.NextUpdate = now + weatherComp.UpdateDelay;
            Dirty(uid, weatherComp);

            // start and end do no damage
            var percent = _weather.GetWeatherPercent((uid, statusComp));
            if (percent < 1f)
                continue;

            var mapEnt = statusComp.AppliedTo ?? uid;
            // ADT-Tweak-Start
            if (weatherComp.Damage is { } damage)
                UpdateDamage(mapEnt, damage, weatherComp.DamageBlacklist);

            if (TryComp<ADTWeatherPushComponent>(uid, out var push))
                UpdatePush((uid, push), mapEnt, now);
            // ADT-Tweak-End
        }
    }

    // ADT-Tweak-
    private void UpdatePush(Entity<ADTWeatherPushComponent> ent, EntityUid map, TimeSpan now)
    {
        if (now >= ent.Comp.NextDirectionChange)
        {
            ent.Comp.Direction = _random.NextAngle();
            ent.Comp.NextDirectionChange = now + TimeSpan.FromSeconds(_random.NextFloat(
                (float) ent.Comp.DirectionChangeMin.TotalSeconds,
                (float) ent.Comp.DirectionChangeMax.TotalSeconds));
        }

        var shove = ent.Comp.Direction.ToVec() * ent.Comp.Strength;

        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var xform))
        {
            if (xform.MapUid != map || mob.CurrentState == MobState.Dead)
                continue;

            if (!IsExposed(xform))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, uid))
                continue;

            _throwing.TryThrow(uid,
                shove,
                ent.Comp.Speed,
                animated: false,
                playSound: false,
                doSpin: false,
                recoil: false);
        }
    }

    private bool IsExposed(TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return true;

        var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
        return _weather.CanWeatherAffect((gridUid, (MapGridComponent?)grid, null), tile);
    }
    // ADT-Tweak-End

    private void UpdateDamage(EntityUid map, DamageSpecifier damage, EntityWhitelist? damageBlacklist)
    {
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var xform))
        {
            // don't give dead bodies 10000 burn, that's not fun for anyone
            if (xform.MapUid != map || mob.CurrentState == MobState.Dead)
                continue;

            if (!IsExposed(xform)) // ADT Tweak
                continue;

            if (!_whitelist.IsWhitelistFailOrNull(damageBlacklist, uid))
                continue;

            _damageable.TryChangeDamage(uid, damage, interruptsDoAfters: false);
        }
    }
}
