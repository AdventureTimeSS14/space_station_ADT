using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Shared.ADT.Lavaland.Puzzle;
using Content.Shared.Camera;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
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
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string PrisonerContainerId = "prisoner";

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
        SubscribeLocalEvent<ADTPrisonCubeComponent, UseInHandEvent>(OnPrisonCubeUse);
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

        var gridEnt = _mapManager.CreateGridEntity(xform.MapID);
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
        for (var spotId = 1; spotId <= 9; spotId++)
        {
            var tile = center + TileOffsets[spotId - 1];

            foreach (var uid in _map.GetAnchoredEntities(gridUid, grid, tile))
            {
                if (HasComp<ADTSlidingPuzzleComponent>(uid) || HasComp<ADTSlidingPuzzleElementComponent>(uid))
                    continue;

                if (TryComp<PhysicsComponent>(uid, out var physics) && physics.BodyType == BodyType.Static)
                    QueueDel(uid);
            }
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

    private void SwapElements(Entity<ADTSlidingPuzzleComponent> puzzle, EntityUid a, EntityUid b)
    {
        var aCoords = Transform(a).Coordinates;
        var bCoords = Transform(b).Coordinates;

        var aXform = Transform(a);
        var bXform = Transform(b);

        _transform.Unanchor(a, aXform);
        _transform.Unanchor(b, bXform);
        _transform.SetCoordinates(a, bCoords);
        _transform.SetCoordinates(b, aCoords);
        _transform.AnchorEntity(a, Transform(a));
        _transform.AnchorEntity(b, Transform(b));
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
        var xform = Transform(element);
        _transform.Unanchor(element, xform);
        _transform.SetCoordinates(element, coords);
        _transform.AnchorEntity(element, Transform(element));

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
        if (puzzle.Comp.MegafaunaProto is { } megafauna &&
            puzzle.Comp.MegafaunaChance > 0f &&
            _random.Prob(puzzle.Comp.MegafaunaChance) &&
            !HasLivingMegafauna(puzzle, megafauna))
        {
            Spawn(megafauna, Transform(puzzle).Coordinates);
            return;
        }

        if (puzzle.Comp.RewardProto is { } reward)
            Spawn(reward, Transform(puzzle).Coordinates);
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
    }

    private void ReleasePrisoner(Entity<ADTSlidingPuzzleComponent> puzzle, EntityUid prisoner)
    {
        if (TerminatingOrDeleted(prisoner))
            return;

        RemComp<BlockMovementComponent>(prisoner);

        if (HasComp<ContainerManagerComponent>(puzzle.Owner))
        {
            _container.RemoveEntity(puzzle.Owner, prisoner, destination: Transform(puzzle).Coordinates);
        }
        else
        {
            _transform.SetCoordinates(prisoner, Transform(puzzle).Coordinates);
        }

        puzzle.Comp.Prisoner = null;

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

    private void OnPrisonCubeUse(Entity<ADTPrisonCubeComponent> cube, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;

        if (!TryComp<MobStateComponent>(user, out var mobState) || mobState.CurrentState != MobState.Alive)
            return;

        var puzzleUid = Spawn(cube.Comp.PuzzleProto, Transform(user).Coordinates);

        if (TerminatingOrDeleted(puzzleUid) ||
            !TryComp<ADTSlidingPuzzleComponent>(puzzleUid, out var puzzle) ||
            puzzle.Elements.Count == 0)
        {
            QueueDel(puzzleUid);
            _popup.PopupEntity(Loc.GetString("prison-cube-no-space"), user, user);
            return;
        }

        Imprison(user, (puzzleUid, puzzle));

        _audio.PlayPvs(cube.Comp.ActivateSound, user);
        _popup.PopupEntity(Loc.GetString("prison-cube-activated"), user, user, PopupType.Medium);

        QueueDel(cube.Owner);

        args.Handled = true;
    }
}