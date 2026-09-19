using Content.Server.Construction.Components;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Content.Shared.Tag;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.RoundEnd;

public sealed class StationIntegritySystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

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

        var grids = EntityQueryEnumerator<StationMemberComponent, MapGridComponent>();
        while (grids.MoveNext(out var gridUid, out _, out var grid))
        {
            var tiles = _map.GetAllTilesEnumerator(gridUid, grid);
            while (tiles.MoveNext(out _))
            {
                state.Floor++;
            }
        }

        var query = EntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out var uid, out var xform))
        {
            if (xform.GridUid is not { } gridUid2 || !HasComp<StationMemberComponent>(gridUid2))
                continue;

            if (HasComp<DoorComponent>(uid))
                state.Door++;
            else if (_tags.HasTag(uid, WallTag))
                state.Wall++;
            else if (_tags.HasTag(uid, WindowTag))
                state.Window++;
            else if (HasComp<MachineComponent>(uid))
                state.Machine++;
        }

        return state;
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
