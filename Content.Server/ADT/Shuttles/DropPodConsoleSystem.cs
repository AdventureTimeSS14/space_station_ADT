using Content.Server.Chat.Systems;
using Content.Server.Decals;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NukeOps;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Stack;
using Content.Shared.ADT.Shuttles.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Station.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Server.Shuttles.Components;
using Content.Shared.Stacks;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Warps;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared.Decals;
using Content.Shared.NukeOps;

namespace Content.Server.ADT.Shuttles;

public sealed class DropPodConsoleSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly DecalSystem _decalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropPodConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleOpened);
        SubscribeLocalEvent<DropPodConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<NukeDropPodComponent, FTLCompletedEvent>(OnDropPodArrived);
        SubscribeLocalEvent<WarDeclaredEvent>(OnWarDeclared);

        Subs.BuiEvents<DropPodConsoleComponent>(DropPodConsoleUiKey.Key, subs =>
        {
            subs.Event<DropPodConsoleDeployMessage>(OnDeployMessage);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NukeDropPodComponent>();
        while (query.MoveNext(out var uid, out var pod))
        {
            if (pod.PendingSpawnAt == null || pod.PendingSpawnCoords == null)
                continue;
            if (_timing.CurTime < pod.PendingSpawnAt.Value)
                continue;

            if (pod.PendingSpawnPrototype != null)
                Spawn(pod.PendingSpawnPrototype, pod.PendingSpawnCoords.Value);
            pod.PendingSpawnAt = null;
            pod.PendingSpawnCoords = null;
            pod.PendingSpawnPrototype = null;
        }
    }

    private void OnWarDeclared(ref WarDeclaredEvent ev)
    {
        var query = EntityQueryEnumerator<DropPodConsoleComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            comp.WarDeclaredTime = ev.Status == WarConditionStatus.WarReady ? _timing.CurTime : null;
        }
    }

    private void OnConsoleInit(Entity<DropPodConsoleComponent> ent, ref ComponentInit args)
    {
        // Set cooldown start time when the console is spawned
        ent.Comp.LastLaunchTime = _timing.CurTime;
    }

    private void OnDropPodArrived(Entity<NukeDropPodComponent> ent, ref FTLCompletedEvent args)
    {
        var podGrid = ent.Owner;
        if (!TryComp<MapGridComponent>(podGrid, out var podGridComp))
            return;

        // Freeze physics immediately so impact forces can't further move/rotate the pod
        _shuttle.Disable(podGrid);
        // Do NOT zero the rotation here — MergeIntoStation reads pod-local coords before reparenting

        var podXformOnArrival = Transform(podGrid);
        var podCoords = podXformOnArrival.Coordinates;
        var podWorldPos = _transform.GetWorldPosition(podXformOnArrival);
        var mapId = podXformOnArrival.MapID;

        // Play loud impact sound at landing site
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_slam5.ogg"), podCoords, AudioParams.Default.WithVolume(12f));
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/explosion3.ogg"), podCoords, AudioParams.Default.WithVolume(10f));

        // Camera shake — everyone within 20 tiles gets their screen kicked
        var epicenter = new MapCoordinates(podWorldPos, mapId);
        var shakeFilter = Filter.Empty().AddInRange(epicenter, 20f, _playerManager, EntityManager);
        foreach (var player in shakeFilter.Recipients)
        {
            if (player.AttachedEntity is not { } shakeTarget)
                continue;
            var delta = _transform.GetWorldPosition(shakeTarget) - podWorldPos;
            if (delta.EqualsApprox(Vector2.Zero))
                delta = new Vector2(0.01f, 0f);
            var dist = delta.Length();
            var intensity = 5f * (1f - dist / 20f);
            if (intensity > 0.01f)
                _recoil.KickCamera(shakeTarget, delta.Normalized() * intensity);
        }

        // Damage and knock back all nearby humanoids; deal structural damage to station entities in blast radius
        var bluntDamage = new DamageSpecifier();
        bluntDamage.DamageDict.Add("Blunt", 50);
        var structuralDamage = new DamageSpecifier();
        structuralDamage.DamageDict.Add("Blunt", 500);

        // Pre-calculate which station tiles the pod will cover, so we can mark entities
        // for suppressed scrap spawning.
        var coveredStationTiles = new HashSet<Vector2i>();
        EntityUid? stationGridForCover = null;
        MapGridComponent? stationCompForCover = null;

        var podTargetGrid = ent.Comp.TargetStationGrid;
        if (podTargetGrid != null && !TerminatingOrDeleted(podTargetGrid.Value) && TryComp<MapGridComponent>(podTargetGrid.Value, out var sgc))
        {
            stationGridForCover = podTargetGrid.Value;
            stationCompForCover = sgc;

            var podWorldMatrix = _transform.GetWorldMatrix(podGrid);
            Matrix3x2.Invert(podWorldMatrix, out var podInvMatrix);
            var stationWorldMatrix = _transform.GetWorldMatrix(stationGridForCover.Value);
            Matrix3x2.Invert(stationWorldMatrix, out var stationInvMatrix);

            var podOriginWorld = _transform.GetWorldPosition(podGrid);
            var podOriginStation = Vector2.Transform(podOriginWorld, stationInvMatrix);
            var baseTile = new Vector2i(
                (int)Math.Round(podOriginStation.X / sgc.TileSize),
                (int)Math.Round(podOriginStation.Y / sgc.TileSize));

            foreach (var tileRef in _mapSystem.GetAllTiles(podGrid, podGridComp))
            {
                var stationTileIdx = baseTile + tileRef.GridIndices;
                coveredStationTiles.Add(stationTileIdx);
            }
        }

        var nearby = _lookup.GetEntitiesInRange(podCoords, 6f,
            LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries);
        foreach (var target in nearby)
        {
            if (target == podGrid) continue;
            var targetXform = Transform(target);

            if (targetXform.GridUid == podGrid)
                continue;

            if (stationGridForCover is { } stationGridCov && stationCompForCover is { } stationCompCov)
            {
                if (targetXform.GridUid == stationGridCov)
                {
                    var tileRef = _mapSystem.GetTileRef(stationGridCov, stationCompCov, targetXform.Coordinates);
                    if (coveredStationTiles.Contains(tileRef.GridIndices))
                    {
                        // Mark for suppressed scrap - entity still destroyed via damage, but no scrap spawns
                        EnsureComp<DroppodSuppressedComponent>(target);
                        _damageable.TryChangeDamage(target, structuralDamage, ignoreResistances: true);
                        continue;
                    }
                }
            }

            if (HasComp<HumanoidProfileComponent>(target))
            {
                // People buckled to a seat are protected
                if (TryComp<BuckleComponent>(target, out var buckle) && buckle.Buckled)
                    continue;

                _damageable.TryChangeDamage(target, bluntDamage, ignoreResistances: false);
                var dir = _transform.GetWorldPosition(targetXform) - podWorldPos;
                if (dir == Vector2.Zero)
                    dir = new Vector2(_random.NextFloat(-1f, 1f), _random.NextFloat(-1f, 1f));
                _throwing.TryThrow(target, Vector2.Normalize(dir), 8f);
            }
            else
            {
                _damageable.TryChangeDamage(target, structuralDamage, ignoreResistances: true);
            }
        }

        // Merge pod into station grid so tiles and walls become part of the station
        var stationGridUid = ent.Comp.TargetStationGrid;
        if (stationGridUid != null
            && !TerminatingOrDeleted(stationGridUid.Value)
            && TryComp<MapGridComponent>(stationGridUid.Value, out var stationGridComp))
        {
            MergeIntoStation(podGrid, podGridComp, stationGridUid.Value, stationGridComp);
        }
        else
        {
            _shuttle.Disable(podGrid);
        }
    }

    /// <summary>
    /// Copies pod tiles onto the station grid and reparents all pod entities, correcting for any
    /// pod rotation so everything lands axis-aligned with the station grid.
    /// </summary>
    private void MergeIntoStation(
        EntityUid podGrid, MapGridComponent podComp,
        EntityUid stationGrid, MapGridComponent stationComp)
    {
        // Build matrices BEFORE any rotation changes so world positions of children are still valid.
        var podWorldMatrix = _transform.GetWorldMatrix(podGrid);
        Matrix3x2.Invert(podWorldMatrix, out var podInvMatrix);
        var stationWorldMatrix = _transform.GetWorldMatrix(stationGrid);
        Matrix3x2.Invert(stationWorldMatrix, out var stationInvMatrix);
        var podWorldRot = Transform(podGrid).WorldRotation;

        // Find which station tile the pod's local origin (0,0) falls in.
        // Use Round (not Floor) to handle floating-point imprecision from FTL placement.
        var podOriginWorld = _transform.GetWorldPosition(podGrid);
        var podOriginStation = Vector2.Transform(podOriginWorld, stationInvMatrix);
        var baseTile = new Vector2i(
            (int)Math.Round(podOriginStation.X / stationComp.TileSize),
            (int)Math.Round(podOriginStation.Y / stationComp.TileSize));

        // 1. Copy tiles using tile-index arithmetic — completely rotation-independent.
        //    Pod tile (i,j) always goes to station tile (base.X+i, base.Y+j).
        var tilesToSet = new List<(Vector2i GridIndices, Tile Tile)>();
        foreach (var tileRef in _mapSystem.GetAllTiles(podGrid, podComp))
        {
            var stationTileIdx = baseTile + tileRef.GridIndices;
            foreach (var stale in _mapSystem.GetAnchoredEntities(stationGrid, stationComp, stationTileIdx).ToList())
                QueueDel(stale);
            tilesToSet.Add((stationTileIdx, tileRef.Tile));
        }
        _mapSystem.SetTiles((stationGrid, stationComp), tilesToSet);
        RemoveDecalsInTileArea(stationGrid, stationComp, tilesToSet);

        // 2. Reparent pod children, correcting for any pod rotation.
        var podXform = Transform(podGrid);
        var toMove = new List<EntityUid>();
        var childEnum = podXform.ChildEnumerator;
        while (childEnum.MoveNext(out var child))
            toMove.Add(child);

        foreach (var child in toMove)
        {
            if (TerminatingOrDeleted(child)) continue;
            var childXform = Transform(child);
            var wasAnchored = childXform.Anchored;

            // Read the entity's pod-local rotation BEFORE reparenting (it will change after).
            var podLocalRot = childXform.LocalRotation;

            // Convert entity world position → pod-local → station-local.
            // This un-rotates the position relative to the pod, giving a clean axis-aligned placement.
            var entityWorldPos = _transform.GetWorldPosition(childXform);
            var entityPodLocal = Vector2.Transform(entityWorldPos, podInvMatrix);
            var entityStationLocal = entityPodLocal + new Vector2(
                baseTile.X * stationComp.TileSize,
                baseTile.Y * stationComp.TileSize);

            _transform.SetCoordinates((child, Transform(child), MetaData(child)), new EntityCoordinates(stationGrid, entityStationLocal), unanchor: false);
            // Restore pod-local rotation (SetCoordinates doesn't preserve it when changing parent).
            _transform.SetLocalRotation(child, podLocalRot);

            if (wasAnchored)
                _transform.AnchorEntity(child, Transform(child));
        }

        // 3. Remove the now-empty pod grid.
        QueueDel(podGrid);
    }

    // Find all grids that belong to stations marked with DropPodTargetStationComponent
    private HashSet<EntityUid> GetValidStationGrids()
    {
        var valid = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<DropPodTargetStationComponent, StationDataComponent>();
        while (query.MoveNext(out _, out var stationData))
        {
            foreach (var gridUid in stationData.Grids)
            {
                if (!TerminatingOrDeleted(gridUid) && HasComp<MapGridComponent>(gridUid))
                    valid.Add(gridUid);
            }
        }
        return valid;
    }

    private void OnConsoleOpened(Entity<DropPodConsoleComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<DropPodConsoleComponent> ent)
    {
        var (uid, comp) = ent;

        var xform = Transform(uid);
        var onDropPod = xform.GridUid != null && HasComp<NukeDropPodComponent>(xform.GridUid.Value);
        var alreadyLaunched = xform.GridUid != null
            && TryComp<NukeDropPodComponent>(xform.GridUid.Value, out var dropPod)
            && dropPod.Launched;

        var isFirstLaunch = comp.LastLaunchTime == TimeSpan.Zero;
        var elapsed = _timing.CurTime - comp.LastLaunchTime;
        var cooldownReady = onDropPod && !alreadyLaunched && elapsed >= comp.Cooldown;

        var isAtWar = false;
        var warCooldownRemaining = 0;
        var warNukieArriveDelay = TimeSpan.Zero;

        var nukeopsQuery = EntityQueryEnumerator<NukeopsRuleComponent>();
        while (nukeopsQuery.MoveNext(out _, out var nukeops))
        {
            if (nukeops.WarDeclaredTime != null)
            {
                var warTime = _timing.CurTime - nukeops.WarDeclaredTime.Value;
                warNukieArriveDelay = nukeops.WarNukieArriveDelay;
                if (warTime < nukeops.WarNukieArriveDelay)
                {
                    isAtWar = true;
                    warCooldownRemaining = (int)Math.Ceiling((nukeops.WarNukieArriveDelay - warTime).TotalSeconds);
                }
                break;
            }
        }

        comp.WarDeclaredTime = isAtWar ? _timing.CurTime : comp.WarDeclaredTime;

        var tcCount = GetTcInSlot(uid);
        var currentCost = isAtWar ? comp.WarCost : comp.PeaceCost;
        var canLaunch = cooldownReady && tcCount >= currentCost;

        var cooldownRemaining = cooldownReady ? 0 : (int)Math.Ceiling((comp.Cooldown - elapsed).TotalSeconds);

        var validStationGrids = GetValidStationGrids();

        var validBeacons = new List<DropPodBeaconInfo>();
        var stationBeacons = new List<Vector2>();

        var beaconQuery = EntityQueryEnumerator<WarpPointComponent, NavMapBeaconComponent, MetaDataComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out _, out var navMap, out var meta))
        {
            var beaconXform = Transform(beaconUid);
            var name = navMap.Text ?? navMap.DefaultText ?? meta.EntityName;
            var worldPos = _transform.GetWorldPosition(beaconUid);
            var prototypeId = meta.EntityPrototype?.ID;

            if (beaconXform.GridUid is null || !validStationGrids.Contains(beaconXform.GridUid.Value))
                continue;

            if (!string.IsNullOrEmpty(name) && !IsBlacklisted(prototypeId, comp.BeaconBlacklist))
            {
                validBeacons.Add(new DropPodBeaconInfo
                {
                    Uid = GetNetEntity(beaconUid),
                    Name = name,
                    WorldPos = worldPos,
                });
            }

            stationBeacons.Add(worldPos);
        }

        var stationCenter = stationBeacons.Count > 0
            ? stationBeacons.Aggregate(Vector2.Zero, (a, b) => a + b) / stationBeacons.Count
            : Vector2.Zero;

        var stationGrid = validStationGrids.Count > 0
            ? GetNetEntity(validStationGrids.First())
            : (NetEntity?) null;

        _ui.SetUiState(uid, DropPodConsoleUiKey.Key, new DropPodConsoleBuiState
        {
            ValidBeacons = validBeacons,
            CanLaunch = canLaunch,
            AlreadyLaunched = alreadyLaunched,
            CooldownRemaining = cooldownRemaining,
            StationGrid = stationGrid,
            StationWorldCenter = stationCenter,
            TcBalance = tcCount,
            CurrentCost = currentCost,
            IsAtWar = isAtWar,
            WarCooldownRemaining = warCooldownRemaining,
        });
    }

    private int GetTcInSlot(EntityUid uid)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var slots))
            return 0;
        var item = _itemSlots.GetItemOrNull(uid, "tcSlot", slots);
        if (item is not { } tcEnt)
            return 0;
        if (!TryComp<StackComponent>(tcEnt, out var stack) || stack.StackTypeId != "Telecrystal")
            return 0;
        return stack.Count;
    }

    private void OnDeployMessage(Entity<DropPodConsoleComponent> ent, ref DropPodConsoleDeployMessage args)
    {
        var (uid, comp) = ent;

        var xform = Transform(uid);
        if (xform.GridUid == null || !TryComp<NukeDropPodComponent>(xform.GridUid.Value, out var dropPod))
        {
            Log.Warning($"DropPodConsole {ToPrettyString(uid)} is not on a DropPod grid.");
            return;
        }

        if (dropPod.Launched)
            return;

        if ((_timing.CurTime - comp.LastLaunchTime) < comp.Cooldown)
            return;

        if (comp.WarDeclaredTime != null)
        {
            var warTime = _timing.CurTime - comp.WarDeclaredTime.Value;
            var warNukieArriveDelay = TimeSpan.FromMinutes(15); // default > NukeopsRuleComponent 15 minutes
            var nukeopsQuery = EntityQueryEnumerator<NukeopsRuleComponent>();
            while (nukeopsQuery.MoveNext(out _, out var nukeops))
            {
                if (nukeops.WarDeclaredTime != null)
                {
                    warNukieArriveDelay = nukeops.WarNukieArriveDelay;
                    break;
                }
            }

            if (warTime < warNukieArriveDelay)
            {
                var timeRemain = warNukieArriveDelay - warTime;
                Log.Debug($"DropPodConsole {ToPrettyString(uid)}: launch blocked by war cooldown, {timeRemain:mm\\:ss} remaining.");
                return;
            }
        }

        var tcCount = GetTcInSlot(uid);

        var isAtWar = comp.WarDeclaredTime != null;
        var currentCost = isAtWar ? comp.WarCost : comp.PeaceCost;

        if (tcCount < currentCost)
            return;

        var targetBeaconEnt = GetEntity(args.TargetBeacon);

        var validStationGrids = GetValidStationGrids();

        if (!TryComp<WarpPointComponent>(targetBeaconEnt, out var warpPoint))
            return;

        if (!TryComp<NavMapBeaconComponent>(targetBeaconEnt, out var navMap))
            return;

        var beaconXform = Transform(targetBeaconEnt);
        if (beaconXform.GridUid is null || !validStationGrids.Contains(beaconXform.GridUid.Value))
        {
            Log.Warning($"DropPodConsole {ToPrettyString(uid)}: target beacon {ToPrettyString(targetBeaconEnt)} is not on a valid station grid.");
            return;
        }

        var beaconName = navMap.Text ?? navMap.DefaultText ?? MetaData(targetBeaconEnt).EntityName;
        var beaconPrototypeId = MetaData(targetBeaconEnt).EntityPrototype?.ID;
        if (string.IsNullOrEmpty(beaconName) || IsBlacklisted(beaconPrototypeId, comp.BeaconBlacklist))
            return;

        var targetWorldPos = _transform.GetWorldPosition(targetBeaconEnt);
        if (beaconXform.MapUid == null)
            return;

        var podGrid = xform.GridUid.Value;
        if (!TryComp<ShuttleComponent>(podGrid, out var shuttle))
        {
            Log.Warning($"DropPodConsole {ToPrettyString(uid)}: drop pod grid {ToPrettyString(podGrid)} has no ShuttleComponent.");
            return;
        }

        // Gather all blacklisted beacon positions for landing offset avoidance
        var blacklistedPositions = new List<Vector2>();
        var blacklistQuery = EntityQueryEnumerator<WarpPointComponent, NavMapBeaconComponent, MetaDataComponent>();
        while (blacklistQuery.MoveNext(out var bUid, out _, out _, out var bMeta))
        {
            var bPrototypeId = bMeta.EntityPrototype?.ID;
            if (IsBlacklisted(bPrototypeId, comp.BeaconBlacklist))
                blacklistedPositions.Add(_transform.GetWorldPosition(bUid));
        }

        // Announce the chosen beacon — the exact tile is still randomised by GetDropPodTargetCoords
        var announcement = Loc.GetString("drop-pod-console-launch-announcement",
            ("beacon", beaconName),
            ("seconds", (int)comp.FlightTime));
        _chat.DispatchGlobalAnnouncement(
            announcement,
            sender: Loc.GetString("drop-pod-console-sender"),
            colorOverride: Color.Red);

        if (TryComp<ItemSlotsComponent>(uid, out var slots))
        {
            var tcEnt = _itemSlots.GetItemOrNull(uid, "tcSlot", slots);
            if (tcEnt is { } tcItem && TryComp<StackComponent>(tcItem, out var stack))
                _stack.TryUse((tcItem, stack), currentCost);
        }

        dropPod.Launched = true;
        comp.LastLaunchTime = _timing.CurTime;
        dropPod.TargetStationGrid = beaconXform.GridUid;

        // Exact landing coords — random 3-8 tile offset, kept away from blacklisted beacons
        var targetCoords = GetDropPodTargetCoords(targetBeaconEnt, targetWorldPos, blacklistedPositions);

        // Schedule the pre-landing effect prototype at the configured lead time
        if (comp.PreLandingSpawnPrototype != null)
        {
            var spawnDelay = Math.Max(0f, comp.FlightTime - comp.PreLandingSpawnLeadTime);
            dropPod.PendingSpawnAt = _timing.CurTime + TimeSpan.FromSeconds(spawnDelay);
            dropPod.PendingSpawnCoords = targetCoords;
            dropPod.PendingSpawnPrototype = comp.PreLandingSpawnPrototype;
        }

        _shuttle.FTLToCoordinates(
            podGrid,
            shuttle,
            targetCoords,
            Angle.Zero,
            startupTime: 5f,
            hyperspaceTime: comp.FlightTime);

        UpdateUiState(ent);
    }

    private EntityCoordinates GetDropPodTargetCoords(EntityUid beaconEnt, Vector2 beaconWorldPos, List<Vector2> blacklistedPositions)
    {
        var beaconXform = Transform(beaconEnt);
        var mapUid = beaconXform.MapUid!.Value;
        var beaconMapCoords = new MapCoordinates(beaconWorldPos, beaconXform.MapID);

        const float MinBlacklistDist = 20f;

        if (_mapManager.TryFindGridAt(beaconMapCoords, out var gridUid, out var gridComp))
        {
            const int maxTries = 24;
            for (var i = 0; i < maxTries; i++)
            {
                var offsetAngle = new Angle(_random.NextDouble() * Math.Tau);
                var offsetDist = _random.NextFloat(3f, 8f);
                var offsetVec = offsetAngle.RotateVec(new Vector2(offsetDist, 0f));
                var testWorldPos = beaconWorldPos + offsetVec;

                // Must be on solid ground
                if (!IsPositionSafeFromSpace(gridUid, gridComp, testWorldPos, 2))
                    continue;

                // Must be at least MinBlacklistDist tiles from every blacklisted beacon
                var tooClose = false;
                foreach (var blackPos in blacklistedPositions)
                {
                    if ((testWorldPos - blackPos).Length() < MinBlacklistDist)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                var tileIdx = _mapSystem.WorldToTile(gridUid, gridComp, testWorldPos);
                var snappedPos = Vector2.Transform(
                    new Vector2(tileIdx.X * gridComp.TileSize, tileIdx.Y * gridComp.TileSize),
                    _transform.GetWorldMatrix(gridUid));
                return new EntityCoordinates(mapUid, snappedPos);
            }

            // Fallback: pick any solid tile near the beacon that isn't blacklisted-adjacent
            for (var i = 0; i < maxTries; i++)
            {
                var offsetAngle = new Angle(_random.NextDouble() * Math.Tau);
                var offsetDist = _random.NextFloat(3f, 8f);
                var offsetVec = offsetAngle.RotateVec(new Vector2(offsetDist, 0f));
                var testWorldPos = beaconWorldPos + offsetVec;
                if (!IsPositionSafeFromSpace(gridUid, gridComp, testWorldPos, 2))
                    continue;
                var tileIdx = _mapSystem.WorldToTile(gridUid, gridComp, testWorldPos);
                var snappedPos = Vector2.Transform(
                    new Vector2(tileIdx.X * gridComp.TileSize, tileIdx.Y * gridComp.TileSize),
                    _transform.GetWorldMatrix(gridUid));
                return new EntityCoordinates(mapUid, snappedPos);
            }
        }

        // Last-resort fallback: snap to beacon tile
        if (_mapManager.TryFindGridAt(beaconMapCoords, out var fallbackGrid, out var fallbackGridComp))
        {
            var tileIdx = _mapSystem.WorldToTile(fallbackGrid, fallbackGridComp, beaconWorldPos);
            var snappedPos = Vector2.Transform(
                new Vector2(tileIdx.X * fallbackGridComp.TileSize, tileIdx.Y * fallbackGridComp.TileSize),
                _transform.GetWorldMatrix(fallbackGrid));
            return new EntityCoordinates(mapUid, snappedPos);
        }

        return new EntityCoordinates(mapUid, beaconWorldPos);
    }

    private bool IsPositionSafeFromSpace(EntityUid gridUid, MapGridComponent grid, Vector2 worldPos, int checkRadius)
    {
        var tileIdx = _mapSystem.WorldToTile(gridUid, grid, worldPos);
        for (var dx = -checkRadius; dx <= checkRadius; dx++)
        {
            for (var dy = -checkRadius; dy <= checkRadius; dy++)
            {
                var neighbor = tileIdx + new Vector2i(dx, dy);
                var tileRef = _mapSystem.GetTileRef(gridUid, grid, neighbor);
                if (_turf.IsSpace(tileRef))
                    return false;
            }
        }
        return true;
    }

    private static bool IsBlacklisted(string? prototypeId, List<string>? blacklist)
    {
        if (prototypeId == null || blacklist == null)
            return false;
        return blacklist.Contains(prototypeId);
    }

    private void RemoveDecalsInTileArea(
        EntityUid stationGrid,
        MapGridComponent stationComp,
        List<(Vector2i GridIndices, Tile Tile)> tilesToSet)
    {
        if (!HasComp<DecalGridComponent>(stationGrid))
            return;

        if (tilesToSet.Count == 0)
            return;

        var tileSize = stationComp.TileSize;
        float minX = tilesToSet.Min(t => t.GridIndices.X) * tileSize;
        float minY = tilesToSet.Min(t => t.GridIndices.Y) * tileSize;
        float maxX = (tilesToSet.Max(t => t.GridIndices.X) + 1) * tileSize;
        float maxY = (tilesToSet.Max(t => t.GridIndices.Y) + 1) * tileSize;
        var bounds = new Box2(minX, minY, maxX, maxY);

        var decals = _decalSystem.GetDecalsIntersecting(stationGrid, bounds);
        foreach (var (decalId, _) in decals)
        {
            _decalSystem.RemoveDecal(stationGrid, decalId);
        }
    }
}
