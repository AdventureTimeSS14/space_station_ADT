using Content.Server.Construction.Components;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.RoundEnd;

public sealed class StationIntegritySystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TagSystem _tags = default!;

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    private readonly HashSet<EntityUid> _stationGrids = new();
    private StationState? _startState;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        _startState = Count();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _startState = null;
    }

    public int GetIntegrity()
    {
        if (_startState is not { } start)
            return 100;

        return Math.Clamp((int) MathF.Round(start.Score(Count()) * 100f), 0, 100);
    }

    private StationState Count()
    {
        var state = new StationState();
        _stationGrids.Clear();

        var grids = EntityQueryEnumerator<StationMemberComponent, MapGridComponent>();
        while (grids.MoveNext(out var gridUid, out _, out var grid))
        {
            _stationGrids.Add(gridUid);

            var tiles = _map.GetAllTilesEnumerator(gridUid, grid);
            while (tiles.MoveNext(out _))
            {
                state.Floor++;
            }
        }

        var doors = EntityQueryEnumerator<DoorComponent, TransformComponent>();
        while (doors.MoveNext(out _, out _, out var xform))
        {
            if (OnStation(xform))
                state.Door++;
        }

        var machines = EntityQueryEnumerator<MachineComponent, TransformComponent>();
        while (machines.MoveNext(out _, out _, out var xform))
        {
            if (OnStation(xform))
                state.Machine++;
        }

        var walls = EntityQueryEnumerator<WallComponent, TransformComponent>();
        while (walls.MoveNext(out var uid, out _, out var xform))
        {
            if (!xform.Anchored || !OnStation(xform) || HasComp<DoorComponent>(uid))
                continue;

            state.Wall++;
        }

        var tagged = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (tagged.MoveNext(out var uid, out var tag, out var xform))
        {
            if (!xform.Anchored)
                continue;

            if (HasComp<WallComponent>(uid) || !_tags.HasTag(tag, WindowTag))
                continue;

            if (!OnStation(xform) || HasComp<DoorComponent>(uid))
                continue;

            state.Window++;
        }

        return state;
    }

    private bool OnStation(TransformComponent xform)
    {
        return xform.GridUid is { } grid && _stationGrids.Contains(grid);
    }

    private sealed class StationState
    {
        public int Floor;
        public int Wall;
        public int Window;
        public int Door;
        public int Machine;

        public float Score(StationState result)
        {
            var output = 0f;
            output += result.Floor / (float) Math.Max(Floor, 1);
            output += result.Wall / (float) Math.Max(Wall, 1);
            output += result.Window / (float) Math.Max(Window, 1);
            output += result.Door / (float) Math.Max(Door, 1);
            output += result.Machine / (float) Math.Max(Machine, 1);
            return output / 5f;
        }
    }
}
