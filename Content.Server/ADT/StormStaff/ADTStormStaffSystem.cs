using System.Linq;
using Content.Shared.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Weather;
using Content.Shared.ADT.StormStaff;
using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.StormStaff;

public sealed class ADTStormStaffSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private WeatherSchedulerSystem _weather = default!;

    private readonly List<ADTStormStaffStrike> _due = new();
    private readonly HashSet<EntityUid> _hit = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTStormStaffComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTStormStaffComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTStormStaffComponent>();

        while (query.MoveNext(out var uid, out var staff))
        {
            if (staff.PendingStrikes.Count == 0)
                continue;

            _due.Clear();

            foreach (var strike in staff.PendingStrikes)
            {
                if (now >= strike.StrikeAt)
                    _due.Add(strike);
            }

            foreach (var strike in _due)
            {
                staff.PendingStrikes.Remove(strike);
                Strike((uid, staff), strike);
            }
        }
    }

    private void OnUseInHand(Entity<ADTStormStaffComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (Transform(args.User).MapUid is not { } map || !TryComp<WeatherSchedulerComponent>(map, out var scheduler))
            return;

        if (scheduler.Stages.Count == 0)
            return;

        var current = (scheduler.Stage - 1 + scheduler.Stages.Count) % scheduler.Stages.Count;

        if (!HasWeather(scheduler, current))
            return;

        var last = current;

        while (HasWeather(scheduler, (last + 1) % scheduler.Stages.Count) && (last + 1) % scheduler.Stages.Count != current)
        {
            last = (last + 1) % scheduler.Stages.Count;
        }

        if (last == current)
        {
            _popup.PopupEntity(Loc.GetString("adt-storm-staff-already-ending"), args.User, args.User);
            return;
        }

        _weather.SetStage(map, scheduler, last, true);

        _popup.PopupEntity(Loc.GetString("adt-storm-staff-dispel-self", ("staff", ent.Owner)), args.User, args.User);
        _popup.PopupEntity(
            Loc.GetString("adt-storm-staff-dispel-others", ("user", args.User), ("staff", ent.Owner)),
            args.User,
            Filter.PvsExcept(args.User),
            true,
            PopupType.Medium);
        _audio.PlayPvs(ent.Comp.DispelSound, args.User, AudioParams.Default.WithVolume(6f));

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):user} dispelled the storm with {ToPrettyString(ent):staff}");
    }

    private void OnAfterInteract(Entity<ADTStormStaffComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == args.User)
            return;

        args.Handled = true;

        if (HasComp<PacifiedComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("adt-storm-staff-pacified"), args.User, args.User);
            return;
        }

        if (_charges.IsEmpty(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("adt-storm-staff-no-charges"), args.User, args.User);
            return;
        }

        var coords = _transform.ToMapCoordinates(args.ClickLocation);

        if (coords.MapId != Transform(args.User).MapID
            || !_map.TryFindGridAt(coords, out var gridUid, out var grid)
            || !_map.TryGetTileRef(gridUid, grid, _map.TileIndicesFor(gridUid, grid, coords), out var tileRef)
            || tileRef.Tile.IsEmpty)
        {
            _popup.PopupEntity(Loc.GetString("adt-storm-staff-cant-here"), args.User, args.User);
            return;
        }

        var tile = tileRef.GridIndices;

        if (ent.Comp.PendingStrikes.Any(strike => strike.Grid == gridUid && strike.Tile == tile))
        {
            _popup.PopupEntity(Loc.GetString("adt-storm-staff-already-targeted"), args.User, args.User);
            return;
        }

        _charges.TryUseCharge(ent.Owner);

        var boosted = Transform(args.User).MapUid is { } map && IsStormActive(map);

        ent.Comp.PendingStrikes.Add(new ADTStormStaffStrike
        {
            Grid = gridUid,
            Tile = tile,
            StrikeAt = _timing.CurTime + ent.Comp.StrikeDelay,
            Boosted = boosted,
            User = args.User,
        });

        var center = _map.GridTileToLocal(gridUid, grid, tile);
        Spawn(ent.Comp.TargetEffect, center);
        _audio.PlayPvs(ent.Comp.TargetSound, ent, AudioParams.Default.WithVolume(-10f));
        _popup.PopupEntity(Loc.GetString("adt-storm-staff-target"), args.User, args.User);
    }

    private void Strike(Entity<ADTStormStaffComponent> ent, ADTStormStaffStrike strike)
    {
        if (!TryComp<MapGridComponent>(strike.Grid, out var grid))
            return;

        var center = _map.GridTileToLocal(strike.Grid, grid, strike.Tile);
        Spawn(ent.Comp.BoltEffect, center);
        _audio.PlayPvs(ent.Comp.BoltSound, center, AudioParams.Default.WithVolume(6f));
        _popup.PopupCoordinates(Loc.GetString("adt-storm-staff-strike"), center, PopupType.MediumCaution);

        _hit.Clear();

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var isCenter = x == 0 && y == 0;

                if (!strike.Boosted && !isCenter)
                    continue;

                var tile = strike.Tile + new Vector2i(x, y);
                Spawn(ent.Comp.ShockEffect, _map.GridTileToLocal(strike.Grid, grid, tile));

                foreach (var hit in _lookup.GetLocalEntitiesIntersecting(strike.Grid, tile, 0f, LookupFlags.Uncontained))
                {
                    if (!_hit.Add(hit))
                        continue;

                    HitEntity(ent, hit, isCenter, strike.Boosted, strike.User);
                }
            }
        }

        var fireRange = strike.Boosted ? ent.Comp.StormFireRange : ent.Comp.FireRange;

        foreach (var flammable in _lookup.GetEntitiesInRange<FlammableComponent>(center, fireRange + 0.5f))
        {
            _flammable.AdjustFireStacks(flammable, ent.Comp.FireStacks, flammable.Comp);
            _flammable.Ignite(flammable, ent.Owner, flammable.Comp);
        }

        if (strike.Boosted)
        {
            _explosion.QueueExplosion(
                _transform.ToMapCoordinates(center),
                "Default",
                ent.Comp.StormExplosionIntensity,
                ent.Comp.StormExplosionSlope,
                ent.Comp.StormExplosionMaxTile,
                ent.Owner,
                canCreateVacuum: false);
        }
    }

    private void HitEntity(Entity<ADTStormStaffComponent> ent, EntityUid hit, bool center, bool boosted, EntityUid? user)
    {
        if (HasComp<MobStateComponent>(hit))
        {
            var damage = (float) ent.Comp.ShockDamage;

            if (!HasComp<HumanoidProfileComponent>(hit))
                damage *= ent.Comp.NonHumanoidMultiplier;

            if (center)
                damage *= ent.Comp.CenterMultiplier;

            if (boosted)
                damage *= ent.Comp.StormMultiplier;

            _popup.PopupEntity(Loc.GetString("adt-storm-staff-hit"), hit, hit, PopupType.LargeCaution);
            _electrocution.TryDoElectrocution(hit, ent.Owner, (int) damage, ent.Comp.ShockStun, true, ignoreInsulation: true);

            if (user != null)
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):user} struck {ToPrettyString(hit):target} with lightning from {ToPrettyString(ent):staff}");

            return;
        }

        if (!ent.Comp.ObjectDamage.Empty)
            _damageable.TryChangeDamage(hit, ent.Comp.ObjectDamage, origin: user);
    }

    private bool IsStormActive(EntityUid map)
    {
        if (!TryComp<WeatherSchedulerComponent>(map, out var scheduler) || scheduler.Stages.Count == 0)
            return false;

        var current = (scheduler.Stage - 1 + scheduler.Stages.Count) % scheduler.Stages.Count;
        return HasWeather(scheduler, current);
    }

    private static bool HasWeather(WeatherSchedulerComponent scheduler, int stage)
    {
        var entry = scheduler.Stages[stage];
        return entry.Weather != null || entry.Variants.Count > 0;
    }
}
