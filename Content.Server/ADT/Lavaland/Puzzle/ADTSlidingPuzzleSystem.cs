using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Shared.ADT.Lavaland.Puzzle;
using Content.Shared.Administration;
using Content.Shared.Camera;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Lavaland.Puzzle;

public sealed class ADTSlidingPuzzleSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string PrisonerContainerId = "prisoner";

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
        SubscribeLocalEvent<ADTSlidingPuzzleComponent, ComponentShutdown>(OnPuzzleShutdown);
        SubscribeLocalEvent<ADTPrisonCubeComponent, UseInHandEvent>(OnPrisonCubeUse);
    }

    private void OnMapInit(Entity<ADTSlidingPuzzleComponent> puzzle, ref MapInitEvent args)
    {
        Setup(puzzle);
    }

    private bool CheckSetupLocation(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        var xform = Transform(puzzle);

        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return false;

        var center = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);

        for (var id = 1; id <= 9; id++)
        {
            var tile = _map.GetTileRef(gridUid, grid, center + TileOffsets[id - 1]);

            if (tile.Tile.IsEmpty)
                return false;
        }

        return true;
    }

    private void Setup(Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        if (puzzle.Comp.Finished || puzzle.Comp.Elements.Count > 0)
            return;

        if (!CheckSetupLocation(puzzle))
        {
            QueueDel(puzzle);
            return;
        }

        var xform = Transform(puzzle);
        var gridUid = xform.GridUid!.Value;
        var grid = _gridQuery.GetComponent(gridUid);
        var center = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);

        var leftIds = new List<int>();
        for (var id = 1; id <= 9; id++)
            leftIds.Add(id);

        puzzle.Comp.EmptyTileId = _random.PickAndTake(leftIds);
        puzzle.Comp.Elements.Clear();

        for (var spotId = 1; spotId <= 9; spotId++)
        {
            if (spotId == puzzle.Comp.EmptyTileId)
                continue;

            var tile = center + TileOffsets[spotId - 1];
            var coords = _map.GridTileToLocal(gridUid, grid, tile);

            var id = _random.PickAndTake(leftIds);
            var element = Spawn($"{puzzle.Comp.ElementProtoPrefix}{id}", coords);
            _transform.AnchorEntity(element, Transform(element));

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
        var piece = Spawn($"{puzzle.Comp.PieceProtoPrefix}{pieceId}", coords);
        _transform.AnchorEntity(piece, Transform(piece));
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

        var center = _map.CoordinatesToTile(gridUid, grid, Transform(puzzle).Coordinates);

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

        if (!TryComp<ADTSlidingPuzzleElementComponent>(a, out var aComp) ||
            !TryComp<ADTSlidingPuzzleElementComponent>(b, out var bComp))
        {
            return;
        }

        (aComp.Id, bComp.Id) = (bComp.Id, aComp.Id);
    }

    private EntityUid? GetElementAt(Entity<ADTSlidingPuzzleComponent> puzzle, int spotId)
    {
        if (!TryGetPuzzleGrid(puzzle, out var gridUid, out var grid))
            return null;

        var center = _map.CoordinatesToTile(gridUid, grid, Transform(puzzle).Coordinates);
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

        if (!TryComp<ADTSlidingPuzzleComponent>(element.Comp.Source, out var puzzle) || puzzle.Finished)
            return;

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

        var center = _map.CoordinatesToTile(gridUid, grid, Transform(puzzle).Coordinates);
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
        if (!TryComp<ADTSlidingPuzzleComponent>(element.Comp.Source, out var puzzle) || puzzle.Finished)
            return;

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

        var center = _map.CoordinatesToTile(gridUid, grid, Transform(puzzle).Coordinates);

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
            var center = _map.CoordinatesToTile(gridUid, grid, Transform(puzzle).Coordinates);
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
        var query = EntityQueryEnumerator<MobStateComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var state, out var meta))
        {
            if (state.CurrentState == MobState.Dead)
                continue;

            if (Transform(uid).MapID != mapId)
                continue;

            if (meta.EntityPrototype?.ID == proto.Id)
                return true;
        }

        return false;
    }

    private void OnPuzzleShutdown(Entity<ADTSlidingPuzzleComponent> puzzle, ref ComponentShutdown args)
    {
        if (puzzle.Comp.Prisoner is { } prisoner)
            ReleasePrisoner(puzzle, prisoner);
    }

    private void ReleasePrisoner(Entity<ADTSlidingPuzzleComponent> puzzle, EntityUid prisoner)
    {
        if (prisoner is not { Valid: true })
            return;

        RemComp<AdminFrozenComponent>(prisoner);

        if (HasComp<ContainerManagerComponent>(puzzle.Owner))
        {
            _container.RemoveEntity(puzzle.Owner, prisoner, destination: Transform(puzzle).Coordinates);
        }
        else
        {
            _transform.SetCoordinates(prisoner, Transform(puzzle).Coordinates);
        }

        puzzle.Comp.Prisoner = null;
        puzzle.Comp.PrisonerContainer = null;

        _popup.PopupEntity(Loc.GetString("prison-cube-released"), prisoner, prisoner);
    }

    public void Imprison(EntityUid prisoner, Entity<ADTSlidingPuzzleComponent> puzzle)
    {
        puzzle.Comp.Prisoner = prisoner;

        EnsureComp<AdminFrozenComponent>(prisoner);

        var container = _container.EnsureContainer<Container>(puzzle.Owner, PrisonerContainerId);
        puzzle.Comp.PrisonerContainer = container;

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

        if (!TryComp<ADTSlidingPuzzleComponent>(puzzleUid, out var puzzle))
        {
            QueueDel(puzzleUid);
            return;
        }

        Imprison(user, (puzzleUid, puzzle));

        _audio.PlayPvs(cube.Comp.ActivateSound, user);
        _popup.PopupEntity(Loc.GetString("prison-cube-activated"), user, user, PopupType.Medium);

        QueueDel(cube.Owner);

        args.Handled = true;
    }
}