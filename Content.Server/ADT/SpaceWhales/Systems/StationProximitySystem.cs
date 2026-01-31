using Content.Server.Popups;
using Content.Server.Station.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;
using Content.Server.ADT.MobCaller;
using System.Linq;
using Robust.Shared.Spawners;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.ADT.SpaceWhale.StationProximity;

public sealed class StationProximitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveSpeed = default!;

    private const float CheckInterval = 60;
    private TimeSpan _nextCheck = TimeSpan.Zero;

    private EntityUid? _mobCaller;
    private bool _spawned = false;

    public override void Initialize()
    {
        base.Initialize();
        _nextCheck = _timing.CurTime + TimeSpan.FromSeconds(CheckInterval);

        SubscribeLocalEvent<SpaceWhaleTargetComponent, MobStateChangedEvent>(OnTargetDeath);
        SubscribeLocalEvent<SpaceWhaleTargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    private void OnTargetDeath(Entity<SpaceWhaleTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        RemComp<SpaceWhaleTargetComponent>(ent.Owner);
    }

    private void OnTargetShutdown(Entity<SpaceWhaleTargetComponent> ent, ref ComponentShutdown args)
    {
        StopFollowing(ent.Owner);
    }

    private void StopFollowing(Entity<SpaceWhaleTargetComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (TryComp<MobCallerComponent>(ent.Comp.MobCaller, out var caller))
        {
            foreach (var item in caller.SpawnedEntities)
            {
                EnsureComp<TimedDespawnComponent>(item).Lifetime = 15f;
                _moveSpeed.ChangeBaseSpeed(item, 11, 30, 1);
                _moveSpeed.RefreshMovementSpeedModifiers(item);
            }
        }

        QueueDel(ent.Comp.MobCaller);
        _spawned = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpaceWhaleTargetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.MobCaller.HasValue)
            {
                RemCompDeferred(uid, comp);
                continue;
            }

            var caller = comp.MobCaller.Value.Comp;
            if (caller.SpawnedEntities.Count > 0)
            {
                _spawned = true;
                break;
            }
        }

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + TimeSpan.FromSeconds(CheckInterval);
        CheckStationProximity();
    }

    private void CheckStationProximity()
    {
        if (!_cfg.GetCVar(ADTCCVars.SpaceWhaleSpawn))
            return;

        var stationQuery = EntityQueryEnumerator<BecomesStationComponent, MapGridComponent>();
        var stations = new Dictionary<EntityUid, (MapGridComponent Grid, TransformComponent Xform)>();

        while (stationQuery.MoveNext(out var uid, out _, out var grid))
        {
            var xform = Transform(uid);
            stations.Add(uid, (grid, xform));
        }

        if (stations.Count == 0)
            return;

        var humanoidQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();
        while (humanoidQuery.MoveNext(out var uid, out _, out var mobState, out var humanoidXform))
        {
            if (mobState.CurrentState != MobState.Alive)
                continue;

            var sameMap = false;
            foreach (var (_, (_, stationXform)) in stations)
            {
                if (stationXform.MapUid != humanoidXform.MapUid)
                    continue;

                sameMap = true;
                break;
            }

            if (!sameMap)
                continue;

            CheckHumanoidProximity(uid, stations, humanoidXform);
        }
    }

    private void CheckHumanoidProximity(EntityUid humanoid,
        Dictionary<EntityUid, (MapGridComponent Grid, TransformComponent Xform)> stations,
        TransformComponent humanoidTransform)
    {
        if (humanoidTransform.GridUid.HasValue && stations.TryGetValue(humanoidTransform.GridUid.Value, out _))
        {
            RemComp<SpaceWhaleTargetComponent>(humanoid);
            return;
        }

        var humanoidWorldPos = _transform.GetWorldPosition(humanoidTransform);
        var closestDistance = float.MaxValue;

        foreach (var (stationUid, (grid, stationXform)) in stations)
        {
            if (stationXform.MapUid != humanoidTransform.MapUid)
                continue;

            var stationWorldPos = _transform.GetWorldPosition(stationXform);
            var distance = (humanoidWorldPos - stationWorldPos).Length();

            if (grid.LocalAABB.Size.Length() > 0)
            {
                var gridRadius = grid.LocalAABB.Size.Length() / 2f; // it needs to be halved to get correct mesurements
                distance = Math.Max(0, distance - gridRadius);
            }

            closestDistance = Math.Min(closestDistance, distance);
        }

        if (closestDistance <= _cfg.GetCVar(ADTCCVars.SpaceWhaleSpawnDistance))
            RemCompDeferred<SpaceWhaleTargetComponent>(humanoid);
        else
            HandleFarFromStation(humanoid);
    }

    private void HandleFarFromStation(EntityUid entity) // basically handles space whale spawnings
    {
        if (_spawned)
            return;

        _popup.PopupEntity(
            Loc.GetString("station-proximity-far-from-station"),
            entity,
            entity,
            PopupType.LargeCaution);

        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Ambience/SpaceWhale/leviathan-appear.ogg"),
            entity,
            AudioParams.Default.WithVolume(1f));

        if (_mobCaller.HasValue)
            return;

        // Spawn a dummy entity at the player's location and lock it onto the player
        _mobCaller = Spawn(null, Transform(entity).Coordinates);
        _transform.SetParent(_mobCaller.Value, entity);
        var mobCaller = new MobCallerComponent()
        {

            SpawnProto = "ADTSpaceLeviathan",
            MaxAlive = 1,
            NeedAnchored = false,
            NeedPower = false,
            MinDistance = 100f,
            SpawnSpacing = TimeSpan.FromSeconds(30),
        };

        AddComp(_mobCaller.Value, mobCaller);

        var targetComp = EnsureComp<SpaceWhaleTargetComponent>(entity);// track the dummy on the player
        targetComp.MobCaller = (_mobCaller.Value, mobCaller);
    }
}
