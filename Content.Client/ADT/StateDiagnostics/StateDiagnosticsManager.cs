using System.Text;
using Content.Shared.ADT.StateDiagnostics;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared.Exceptions;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.ADT.StateDiagnostics;

public sealed class StateDiagnosticsManager
{
    [Dependency] private readonly IClientGameStateManager _gameState = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

    private const int HistorySize = 10;
    private const int MaxRuntimeLogLength = 8000;

    private readonly Queue<FullStateRecord> _fullStates = new();
    private ISawmill _sawmill = default!;

    private int _deltasSinceFull;
    private int _maxBufferSinceFull;
    private GameTick _lastFrom;
    private GameTick _lastTo;
    private TimeSpan _lastAppliedAt;
    private int _reportedExceptions;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("adt.state_diag");

        _net.RegisterNetMessage<MsgStateDiagnosticsRequest>(OnRequest);
        _net.RegisterNetMessage<MsgStateDiagnosticsReport>();

        _gameState.GameStateApplied += OnStateApplied;
    }

    private void OnStateApplied(GameStateAppliedArgs args)
    {
        var state = args.AppliedState;
        var now = _timing.RealTime;

        if (state.FromSequence == GameTick.Zero)
        {
            _fullStates.Enqueue(new FullStateRecord(state.ToSequence, now, _deltasSinceFull));

            while (_fullStates.Count > HistorySize)
            {
                _fullStates.Dequeue();
            }

            _deltasSinceFull = 0;
            _maxBufferSinceFull = 0;
        }
        else
        {
            _deltasSinceFull++;
        }

        _maxBufferSinceFull = Math.Max(_maxBufferSinceFull, _gameState.StateCount);
        _lastFrom = state.FromSequence;
        _lastTo = state.ToSequence;
        _lastAppliedAt = now;
    }

    private void OnRequest(MsgStateDiagnosticsRequest msg)
    {
        var report = BuildReport(msg);
        _sawmill.Warning(report);

        if (report.Length > StateDiagnosticsHelper.MaxReportLength)
            report = report.Substring(0, StateDiagnosticsHelper.MaxReportLength);

        _net.ClientSendMessage(new MsgStateDiagnosticsReport { Report = report });
    }

    private string BuildReport(MsgStateDiagnosticsRequest msg)
    {
        var sb = new StringBuilder();
        var now = _timing.RealTime;

        sb.AppendLine($"Server saw {msg.Requests} full state requests in {msg.Window:0}s.");
        sb.AppendLine($"Ticks: cur={_timing.CurTick} lastReal={_timing.LastRealTick} lastProcessed={_timing.LastProcessedTick}");
        sb.AppendLine($"State buffer: count={_gameState.StateCount} applicable={_gameState.GetApplicableStateCount()} target={_gameState.TargetBufferSize} maxSinceLastFull={_maxBufferSinceFull}");
        sb.AppendLine($"Last applied state: {_lastFrom} -> {_lastTo}, {(now - _lastAppliedAt).TotalSeconds:0.0}s ago. Deltas applied since last full state: {_deltasSinceFull}");

        sb.AppendLine("Applied full states (tick, seconds ago, deltas applied before it):");
        foreach (var record in _fullStates)
        {
            sb.AppendLine($"  {record.Tick}, {(now - record.Time).TotalSeconds:0.0}, {record.DeltasBefore}");
        }

        AppendRuntimeExceptions(sb);

        if (_player.LocalEntity is { } local)
            sb.AppendLine($"Local entity: {StateDiagnosticsHelper.DescribeParentChain(_entMan, local)}");
        else
            sb.AppendLine("No local entity.");

        var entities = new List<EntityUid>();
        var query = _entMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            entities.Add(uid);
        }

        StateDiagnosticsHelper.AppendEntityScan(_entMan, entities, sb, true);

        return sb.ToString();
    }

    private void AppendRuntimeExceptions(StringBuilder sb)
    {
        var count = _runtimeLog.ExceptionCount;
        sb.AppendLine($"Runtime exceptions: total={count}, new since last report={count - _reportedExceptions}");

        if (count > _reportedExceptions)
        {
            var text = _runtimeLog.Display();
            if (text.Length > MaxRuntimeLogLength)
                text = text.Substring(text.Length - MaxRuntimeLogLength);

            sb.AppendLine(text);
        }

        _reportedExceptions = count;
    }

    private readonly record struct FullStateRecord(GameTick Tick, TimeSpan Time, int DeltasBefore);
}
