using System.Linq;
using System.Text;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.StateDiagnostics;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.StateDiagnostics;

public sealed class StateDiagnosticsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IServerGameStateManager _gameState = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private readonly Dictionary<NetUserId, SessionDiagnostics> _sessions = new();

    private bool _enabled;
    private int _threshold;
    private TimeSpan _window;
    private TimeSpan _cooldown;
    private float _radius;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<MsgStateDiagnosticsRequest>();
        _net.RegisterNetMessage<MsgStateDiagnosticsReport>(OnReport);

        if (_gameState is ServerGameStateManager manager)
            manager.ClientRequestFull += OnClientRequestFull;
        else
            Log.Error($"Unexpected {nameof(IServerGameStateManager)} implementation, full state requests will not be tracked.");

        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        Subs.CVar(_cfg, ADTCCVars.StateDiagnosticsEnabled, value => _enabled = value, true);
        Subs.CVar(_cfg, ADTCCVars.StateDiagnosticsRequestThreshold, value => _threshold = Math.Max(1, value), true);
        Subs.CVar(_cfg, ADTCCVars.StateDiagnosticsWindow, value => _window = TimeSpan.FromSeconds(value), true);
        Subs.CVar(_cfg, ADTCCVars.StateDiagnosticsCooldown, value => _cooldown = TimeSpan.FromSeconds(value), true);
        Subs.CVar(_cfg, ADTCCVars.StateDiagnosticsRadius, value => _radius = value, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_gameState is ServerGameStateManager manager)
            manager.ClientRequestFull -= OnClientRequestFull;

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _sessions.Remove(args.Session.UserId);
    }

    private void OnClientRequestFull(ICommonSession session, GameTick tick, NetEntity? missingEntity)
    {
        if (!_enabled)
            return;

        if (!_sessions.TryGetValue(session.UserId, out var data))
        {
            data = new SessionDiagnostics();
            _sessions[session.UserId] = data;
        }

        var now = _timing.RealTime;
        data.Requests.Enqueue(new FullStateRequest(now, tick, _timing.CurTick));

        while (data.Requests.Count > 0 && now - data.Requests.Peek().Time > _window)
        {
            data.Requests.Dequeue();
        }

        if (data.Requests.Count < _threshold)
            return;

        if (data.LastDump is { } lastDump && now - lastDump < _cooldown)
            return;

        data.LastDump = now;
        data.AwaitingReport = true;

        Log.Warning(BuildServerReport(session, data, missingEntity));

        _net.ServerSendMessage(new MsgStateDiagnosticsRequest
        {
            Requests = data.Requests.Count,
            Window = (float) _window.TotalSeconds,
        }, session.Channel);
    }

    private void OnReport(MsgStateDiagnosticsReport msg)
    {
        if (!_player.TryGetSessionByChannel(msg.MsgChannel, out var session)
            || !_sessions.TryGetValue(session.UserId, out var data)
            || !data.AwaitingReport)
            return;

        data.AwaitingReport = false;

        var report = msg.Report;
        if (report.Length > StateDiagnosticsHelper.MaxReportLength)
            report = report.Substring(0, StateDiagnosticsHelper.MaxReportLength);

        Log.Warning($"Client report from {session}:\n{report}");
    }

    private string BuildServerReport(ICommonSession session, SessionDiagnostics data, NetEntity? missingEntity)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{session} requested a full game state {data.Requests.Count} times in the last {_window.TotalSeconds:0}s. Server tick: {_timing.CurTick}. Ping: {session.Channel.Ping}ms.");
        sb.AppendLine($"Client ticks (requested from / server tick at request): {string.Join(", ", data.Requests.Select(r => $"{r.ClientTick}/{r.ServerTick}"))}");

        if (missingEntity is { } missing)
            sb.AppendLine($"Client reported an entity without metadata: {EntityManager.ToPrettyString(missing)}");

        if (session.AttachedEntity is not { } attached)
        {
            sb.AppendLine("No attached entity.");
            return sb.ToString();
        }

        sb.AppendLine($"Attached: {StateDiagnosticsHelper.DescribeParentChain(EntityManager, attached)}");

        var nearby = _lookup.GetEntitiesInRange(_xform.GetMapCoordinates(attached), _radius, LookupFlags.All);
        nearby.Add(attached);

        sb.AppendLine($"Entities within {_radius:0} tiles:");
        StateDiagnosticsHelper.AppendEntityScan(EntityManager, nearby, sb, false);

        return sb.ToString();
    }

    private sealed class SessionDiagnostics
    {
        public readonly Queue<FullStateRequest> Requests = new();
        public TimeSpan? LastDump;
        public bool AwaitingReport;
    }

    private readonly record struct FullStateRequest(TimeSpan Time, GameTick ClientTick, GameTick ServerTick);
}
