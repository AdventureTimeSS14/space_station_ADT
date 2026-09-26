using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Shared.ADT.Lavaland.Puzzle;
using Content.Shared.Camera;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Lavaland.Puzzle;

public sealed class ADTSlidingPuzzleSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string PrisonerContainerId = "prisoner";

    private const float PushRadius = 3f;
    private const float PushDistance = 3.5f;
    private const float ThrowSpeed = 20f;

    private const string FloorTile = "ADTFloorCult";

    private static readonly Vector2i[] TileOffsets =
    {
        new(-1, 1), // 1 NW
        new(0, 1),  // 2 N
        new(1, 1),  // 3 NE
        new(-1, 0), // 4 W
        new(0, 0),  // 5
        new(1, 0),  // 6 E
        new(-1, -1),// 7 SW
        new(0, -1), // 8 S
        new(1, -1), // 9 SE
    };

    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<ADTSlidingPuzzleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTSlidingPuzzleElementComponent, InteractHandEvent>(OnElementInteract);
        SubscribeLocalEvent<ADTSlidingPuzzleElementComponent, ComponentShutdown>(OnElementShutdown);
        SubscribeLocalEvent<ADTSlidingPuzzleComponent, EntityTerminatingEvent>(OnPuzzleTerminating);
        SubscribeLocalEvent<ADTPrisonCubeComponent, AfterInteractEvent>(OnPrisonCubeUse);
    }

    private void OnMapInit(Entity<ADTSlidingPuzzleComponent> puzzle, ref MapInitEvent args)
    {
        Setup(puzzle);
    }

    private bool EnsureSetupGrid(
        Entity<ADTSlidingPuzzleComponent> puzzle,
        out EntityUid gridUid,
        out MapGridComponent grid,
        out Vector2i center)
    {
        var xform = Transform(puzzle);

        if (xform.GridUid is { } existingGrid && _gridQuery.TryComp(existingGrid, out var existingComp))
        {
            gridUid = existingGrid;
            grid = existingComp;
            center = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);
            return true;
        }

        if (xform.MapID == MapId.Nullspace)
        {
            gridUid = default;
            grid = default!;
            center = default;
            return false;
        }

        if (_mapManager.TryFindGridAt(_transform.GetMapCoordinates(puzzle), out var foundGrid, out var foundComp))
        {
            gridUid = foundGrid;
            grid = foundComp;
            var tile = _map.WorldToTile(gridUid, grid, xform.WorldPosition);
            _transform.SetCoordinates(puzzle, _map.GridTileToLocal(gridUid, grid, tile));
            center = tile;
            return true;
        }

        var gridEnt = _mapManager.CreateGridEntity(xform.MapID);
        puzzle.Comp.GeneratedGrid = gridEnt;
        var gridXform = Transform(gridEnt);
        _transform.SetWorldPosition((gridEnt, gridXform), _transform.GetMapCoordinates(puzzle).Position);
        _transform.SetCoordinates(puzzle, new EntityCoordinates(gridEnt, Vector2.Zero));

        gridUid = gridEnt;
        grid = gridEnt.Comp;
        center = Vector2i.Zero;
        return true;
    }

    private void EnsurePuzzleFloor(EntityUid gridUid, MapGridComponent grid, Vector2i center)
    {
        var floor = _tileDefinitionManager[FloorTile];

        for (var spotId = 1; spotId <= 9; spotId++)
        {
            var tile = center + TileOffsets[spotId - 1];

            if (_map.GetTileRef(gridUid, grid, tile).Tile.IsEmpty)
                _map.SetTile(gridUid, grid, tile, new Tile(floor.TileId));
        }
    }

    private void ClearPuzzleTiles(EntityUid gridUid, MapGridComponent grid, Vector2i center)
    {
        var anchored = new List<EntityUid>();

        for (var spotId = 1; spotId <= 9; spotId++)
        {
            var tile = center + TileOffsets[spotId - 1];

            _map.GetAnchoredEntities(new Entity<MapGridComponent>(gridUid, grid), tile, anchored);

            foreach (var uid in anchored)
            {
                if (HasComp<ADTSlidingPuzzleComponent>(uid) || HasComp<ADTSlidingPuzzleElementComponent>(uid))
                    continue;

                if (TryComp<PhysicsComponent>(uid, out var physics) && physics.BodyType == BodyType.Static)
                    Del(uid);
            }

            anchored.Clear();
        }
    }

    private void Setup(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        if (puzzle.Comp.Finished || puzzle.Comp.Elements.Count > 0)
            return;

        if (!EnsureSetupGrid(puzzle, out var gridUid, out var grid, out var center))
        {
            QueueDel(puzzle);
            return;
        }

        EnsurePuzzleFloor(gridUid, grid, center);
        ClearPuzzleTiles(gridUid, grid, center);

        var leftIds = new List<int>();
        for (var id = 1; id <= 9; id++)
            leftIds.Add(id);

        var emptyTileId = puzzle.Comp.EmptyTileId;
        if (emptyTileId is >= 1 and <= 9)
            leftIds.Remove(emptyTileId);
        else
            emptyTileId = _random.PickAndTake(leftIds);

        puzzle.Comp.EmptyTileId = emptyTileId;
        puzzle.Comp.Elements.Clear();

        for (var spotId = 1; spotId <= 9; spotId++)
        {
            if (spotId == puzzle.Comp.EmptyTileId)
                continue;

            var tile = center + TileOffsets[spotId - 1];
            var coords = _map.GridTileToLocal(gridUid, grid, tile);

            var id = _random.PickAndTake(leftIds);
            var element = Spawn($"{puzzle.Comp.ElementProtoPrefix}{id}", coords);

            var comp = EnsureComp<ADTSlidingPuzzleElementComponent>(element);
            comp.Source = puzzle;
            comp.Id = id;

            puzzle.Comp.Elements.Add(element);
        }

        if (!IsSolvable(puzzle))
            MakeSolvable(puzzle);
    }

    private void SpawnFloorPiece(Entity<ADTSlidingPuzzleComponent> puzzle, int pieceId, Vector2i tile)
    {
        if (!TryGetPuzzleGrid(puzzle, out var gridUid, out var grid))
            return;

        var coords = _map.GridTileToLocal(gridUid, grid, tile);
        Spawn($"{puzzle.Comp.PieceProtoPrefix}{pieceId}", coords);
    }

    private bool IsSolvable(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        var ordering = ElementsInOrder(puzzle);
        var swapTally = 0;

        for (var i = 0; i < ordering.Count; i++)
        {
            var checkedValue = ordering[i];
            for (var j = i; j < ordering.Count; j++)
            {
                if (ordering[j] < checkedValue)
                    swapTally++;
            }
        }

        return swapTally % 2 == 0;
    }

    private List<int> ElementsInOrder(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        var result = new List<int>(8);

        if (!TryGetPuzzleGrid(puzzle, out var gridUid, out var grid))
            return result;

        var center = GetCenterTile(puzzle, gridUid, grid);

        for (var spotId = 1; spotId <= 9; spotId++)
        {
            if (spotId == puzzle.Comp.EmptyTileId)
                continue;

            var tile = center + TileOffsets[spotId - 1];

            if (GetElementAtTile(puzzle, gridUid, grid, tile) is { } element &&
                TryComp<ADTSlidingPuzzleElementComponent>(element, out var elementComp))
            {
                result.Add(elementComp.Id);
            }
        }

        return result;
    }

    private void MakeSolvable(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        var firstId = 1;
        var otherId = 2;

        if (puzzle.Comp.EmptyTileId is 1 or 2)
        {
            firstId = 4;
            otherId = 5;
        }

        var elementA = GetElementAt(puzzle, firstId);
        var elementB = GetElementAt(puzzle, otherId);

        if (elementA is not { } a || elementB is not { } b)
            return;

        if (!TryComp<ADTSlidingPuzzleElementComponent>(a, out _) ||
            !TryComp<ADTSlidingPuzzleElementComponent>(b, out _))
        {
            return;
        }

        SwapElements(puzzle, a, b);
    }

    private void MoveElementTo(EntityUid element, EntityCoordinates coords)
    {
        var xform = Transform(element);
        _transform.Unanchor(element, xform);
        _transform.SetCoordinates(element, coords);
        _transform.AnchorEntity(element, Transform(element));
    }

    private void SwapElements(Entity<ADTSlidingPuzzleComponent> puzzle, EntityUid a, EntityUid b)
    {
        var aCoords = Transform(a).Coordinates;
        var bCoords = Transform(b).Coordinates;

        MoveElementTo(a, bCoords);
        MoveElementTo(b, aCoords);
    }

    private Vector2i GetCenterTile(
        Entity<ADTSlidingPuzzleComponent> puzzle,
        EntityUid gridUid,
        MapGridComponent grid)
    {
        return _map.CoordinatesToTile(gridUid, grid, Transform(puzzle).Coordinates);
    }

    private EntityUid? GetElementAt(Entity<ADTSlidingPuzzleComponent> puzzle, int spotId)
    {
        if (!TryGetPuzzleGrid(puzzle, out var gridUid, out var grid))
            return null;

        var center = GetCenterTile(puzzle, gridUid, grid);
        return GetElementAtTile(puzzle, gridUid, grid, center + TileOffsets[spotId - 1]);
    }

    private EntityUid? GetElementAtTile(
        Entity<ADTSlidingPuzzleComponent> puzzle,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile)
    {
        foreach (var element in puzzle.Comp.Elements)
        {
            if (!TryComp<ADTSlidingPuzzleElementComponent>(element, out _))
                continue;

            if (_map.CoordinatesToTile(gridUid, grid, Transform(element).Coordinates) == tile)
                return element;
        }

        return null;
    }

    private bool TryGetPuzzleGrid(
        Entity<ADTSlidingPuzzleComponent> puzzle,
        out EntityUid gridUid,
        out MapGridComponent grid)
    {
        if (Transform(puzzle).GridUid is { } uid && _gridQuery.TryComp(uid, out var gridComp))
        {
            gridUid = uid;
            grid = gridComp;
            return true;
        }

        gridUid = default;
        grid = default!;
        return false;
    }

    private void OnElementInteract(Entity<ADTSlidingPuzzleElementComponent> element, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (TerminatingOrDeleted(element.Comp.Source) ||
            !TryComp<ADTSlidingPuzzleComponent>(element.Comp.Source, out var puzzle) ||
            puzzle.Finished)
        {
            return;
        }

        var puzzleEnt = (element.Comp.Source, puzzle);

        if (!TryMoveToEmptySpot(element, puzzleEnt))
        {
            _popup.PopupEntity(Loc.GetString("sliding-puzzle-blocked"), element, args.User);
            return;
        }

        args.Handled = true;
        Validate(puzzleEnt);
    }

    private bool TryMoveToEmptySpot(
        Entity<ADTSlidingPuzzleElementComponent> element,
        Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        if (!TryGetPuzzleGrid(puzzle, out var gridUid, out var grid))
            return false;

        var center = GetCenterTile(puzzle, gridUid, grid);
        var current = _map.CoordinatesToTile(gridUid, grid, Transform(element).Coordinates);

        var offset = current - center;
        if (Math.Abs(offset.X) > 1 || Math.Abs(offset.Y) > 1)
            return false;

        Vector2i? emptySpot = null;

        for (var spotId = 1; spotId <= 9; spotId++)
        {
            var tile = center + TileOffsets[spotId - 1];

            if (tile == current)
                continue;

            if (GetElementAtTile(puzzle, gridUid, grid, tile) is null)
            {
                emptySpot = tile;
                break;
            }
        }

        if (emptySpot is not { } target)
            return false;

        var diff = target - current;
        if (Math.Abs(diff.X) + Math.Abs(diff.Y) != 1)
            return false;

        var coords = _map.GridTileToLocal(gridUid, grid, target);
        MoveElementTo(element, coords);

        return true;
    }

    private void OnElementShutdown(Entity<ADTSlidingPuzzleElementComponent> element, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(element.Comp.Source) ||
            !TryComp<ADTSlidingPuzzleComponent>(element.Comp.Source, out var puzzle) ||
            puzzle.Finished)
        {
            return;
        }

        puzzle.Elements.Remove(element);
        Validate((element.Comp.Source, puzzle));
    }

    private void Validate(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        if (puzzle.Comp.Finished)
            return;

        if (puzzle.Comp.Elements.Count < 8)
        {
            QueueDel(puzzle);
            return;
        }

        if (!TryGetPuzzleGrid(puzzle, out var gridUid, out var grid))
            return;

        var center = GetCenterTile(puzzle, gridUid, grid);

        for (var id = 1; id <= 9; id++)
        {
            var target = center + TileOffsets[id - 1];
            var element = GetElementAtTile(puzzle, gridUid, grid, target);

            if (id == puzzle.Comp.EmptyTileId)
            {
                if (element is not null)
                    return;

                continue;
            }

            if (element is not { } uid ||
                !TryComp<ADTSlidingPuzzleElementComponent>(uid, out var elementComp) ||
                elementComp.Id != id)
            {
                return;
            }
        }

        Finish(puzzle);
    }

    private void Finish(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        puzzle.Comp.Finished = true;

        var mapCoords = _transform.GetMapCoordinates(puzzle);
        var inRange = _lookup.GetEntitiesInRange(mapCoords, puzzle.Comp.RecoilRadius);

        foreach (var uid in inRange)
        {
            if (!TryComp<CameraRecoilComponent>(uid, out var recoil))
                continue;

            var strength = puzzle.Comp.RecoilStrength;
            _recoil.KickCamera(uid, new Vector2(_random.NextFloat(-strength, strength), _random.NextFloat(-strength, strength)), recoil);
        }

        var hasGrid = TryGetPuzzleGrid(puzzle, out var gridUid, out var grid);

        foreach (var element in puzzle.Comp.Elements)
        {
            if (!TryComp<ADTSlidingPuzzleElementComponent>(element, out var elementComp))
                continue;

            if (hasGrid)
            {
                var tile = _map.CoordinatesToTile(gridUid, grid, Transform(element).Coordinates);
                SpawnFloorPiece(puzzle, elementComp.Id, tile);
            }

            QueueDel(element);
        }

        puzzle.Comp.Elements.Clear();

        if (hasGrid)
        {
            var center = GetCenterTile(puzzle, gridUid, grid);
            SpawnFloorPiece(puzzle, puzzle.Comp.EmptyTileId, center + TileOffsets[puzzle.Comp.EmptyTileId - 1]);
        }

        if (puzzle.Comp.SolveSound is { } sound)
            _audio.PlayPvs(sound, puzzle);

        DispenseReward(puzzle);

        _popup.PopupEntity(Loc.GetString("sliding-puzzle-solved"), puzzle, PopupType.Medium);

        if (puzzle.Comp.Prisoner is { } prisoner)
            ReleasePrisoner(puzzle, prisoner);
    }

    private void DispenseReward(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        if (puzzle.Comp.RewardProto is { } reward)
            Spawn(reward, Transform(puzzle).Coordinates);

        if (puzzle.Comp.MegafaunaProto is { } megafauna &&
            puzzle.Comp.MegafaunaChance > 0f &&
            _random.Prob(puzzle.Comp.MegafaunaChance) &&
            !HasLivingMegafauna(puzzle, megafauna))
        {
            Spawn(megafauna, Transform(puzzle).Coordinates);
        }
    }

    private bool HasLivingMegafauna(Entity<ADTSlidingPuzzleComponent> puzzle, EntProtoId proto)
    {
        var mapId = Transform(puzzle).MapID;
        var prototype = _prototype.Index(proto);
        var query = EntityQueryEnumerator<MobStateComponent>();

        while (query.MoveNext(out var uid, out var state))
        {
            if (state.CurrentState == MobState.Dead || TerminatingOrDeleted(uid))
                continue;

            if (Transform(uid).MapID != mapId)
                continue;

            if (MetaData(uid).EntityPrototype == prototype)
                return true;
        }

        return false;
    }

    private void OnPuzzleTerminating(Entity<ADTSlidingPuzzleComponent> puzzle, ref EntityTerminatingEvent args)
    {
        if (puzzle.Comp.Prisoner is { } prisoner)
            ReleasePrisoner(puzzle, prisoner);

        foreach (var element in puzzle.Comp.Elements)
        {
            if (Exists(element))
                QueueDel(element);
        }

        if (puzzle.Comp.GeneratedGrid is { } grid && Exists(grid))
            QueueDel(grid);
    }

    private void ReleasePrisoner(Entity<ADTSlidingPuzzleComponent> puzzle, EntityUid prisoner)
    {
        if (TerminatingOrDeleted(prisoner))
            return;

        RemComp<BlockMovementComponent>(prisoner);
        RemComp<GodmodeComponent>(prisoner);

        if (HasComp<ContainerManagerComponent>(puzzle.Owner))
        {
            _container.RemoveEntity(puzzle.Owner, prisoner, destination: Transform(puzzle).Coordinates);
        }
        else
        {
            _transform.SetCoordinates(prisoner, Transform(puzzle).Coordinates);
        }

        puzzle.Comp.Prisoner = null;

        if (puzzle.Comp.ReturnCubeProto is { } cubeProto)
            Spawn(cubeProto, Transform(puzzle).Coordinates);

        _popup.PopupEntity(Loc.GetString("prison-cube-released"), prisoner, prisoner);
    }

    public void Imprison(EntityUid prisoner, Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        puzzle.Comp.Prisoner = prisoner;

        EnsureComp<BlockMovementComponent>(prisoner);

        var container = _container.EnsureContainer<Container>(puzzle.Owner, PrisonerContainerId);
        _container.Insert(prisoner, container);

        _adminLog.Add(LogType.Mind, LogImpact.Extreme,
            $"{ToPrettyString(prisoner)} was imprisoned inside {ToPrettyString(puzzle.Owner)}");
    }

    private void PushAwayNearbyMobs(EntityUid target)
    {
        var mapCoords = _transform.GetMapCoordinates(target);
        var inRange = _lookup.GetEntitiesInRange(mapCoords, PushRadius);

        foreach (var uid in inRange)
        {
            if (uid == target || !HasComp<MobStateComponent>(uid))
                continue;

            if (_container.IsEntityInContainer(uid))
                continue;

            var direction = _transform.GetMapCoordinates(uid).Position - mapCoords.Position;
            if (direction.LengthSquared() < 0.01f)
                direction = new Vector2(_random.NextFloat(-1f, 1f), _random.NextFloat(-1f, 1f));

            _throwing.TryThrow(uid, direction.Normalized() * PushDistance, ThrowSpeed, playSound: false, doSpin: false);
        }
    }

    private void OnPrisonCubeUse(Entity<ADTPrisonCubeComponent> cube, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (target == args.User)
            return;

        if (HasComp<MechComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("prison-cube-mech"), args.User, args.User);
            return;
        }

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return;

        var isCuffed = TryComp<CuffableComponent>(target, out var cuffable) && cuffable.CuffedHandCount > 0;
        if (!isCuffed && mobState.CurrentState is not (MobState.Critical or MobState.Dead))
        {
            _popup.PopupEntity(Loc.GetString("prison-cube-invalid-state"), args.User, args.User);
            return;
        }

        var puzzleUid = Spawn(cube.Comp.PuzzleProto, Transform(target).Coordinates);

        if (TerminatingOrDeleted(puzzleUid) ||
            !TryComp<ADTSlidingPuzzleComponent>(puzzleUid, out var puzzle) ||
            puzzle.Elements.Count == 0)
        {
            QueueDel(puzzleUid);
            _popup.PopupEntity(Loc.GetString("prison-cube-no-space"), args.User, args.User);
            return;
        }

        Imprison(target, (puzzleUid, puzzle));

        _damageable.ClearAllDamage(target);
        EnsureComp<GodmodeComponent>(target);
        PushAwayNearbyMobs(target);

        _audio.PlayPvs(cube.Comp.ActivateSound, target);
        _popup.PopupEntity(Loc.GetString("prison-cube-activated"), target, target, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("prison-cube-sealed-user", ("target", target)), args.User, args.User);

        QueueDel(cube.Owner);

        args.Handled = true;
    }
}