using System.Linq;
using Content.Server.Connection;
using Content.Shared.CCVar;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.JoinQueue;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.JoinQueue;

public sealed class JoinQueueManager
{
    private static readonly Gauge QueueCount = Metrics.CreateGauge(
        "join_queue_count",
        "Amount of players in queue.");

    private static readonly Counter QueueBypassCount = Metrics.CreateCounter(
        "join_queue_bypass_count",
        "Amount of players who bypassed queue by privileges.");

    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IConnectionManager _connectionManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    private readonly List<ICommonSession> _queue = new();
    private bool _isEnabled;

    public int ActualPlayersCount => _playerManager.PlayerCount - _queue.Count;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgQueueUpdate>();
        _cfg.OnValueChanged(CCCVars.QueueEnabled, OnQueueCVarChanged, true);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnQueueCVarChanged(bool value)
    {
        _isEnabled = value;

        if (!value)
        {
            foreach (var session in _queue.ToArray())
            {
                _queue.Remove(session);
                SendToGame(session);
            }
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            if (!_isEnabled)
            {
                SendToGame(e.Session);
                return;
            }

            var isPrivileged = await _connectionManager.HavePrivilegedJoin(e.Session.UserId);
            var softMax = _cfg.GetCVar(CCVars.SoftMaxPlayers);
            var online = ActualPlayersCount - 1;
            var freeSlot = online < softMax;

            if (isPrivileged || freeSlot)
            {
                SendToGame(e.Session);

                if (isPrivileged && !freeSlot)
                    QueueBypassCount.Inc();

                return;
            }

            _queue.Add(e.Session);
            SendQueueUpdates();
            QueueCount.Set(_queue.Count);
        }

        if (e.NewStatus == SessionStatus.Disconnected)
        {
            var removed = _queue.Remove(e.Session);

            ProcessQueue();
        }
    }

    private void ProcessQueue()
    {
        if (!_isEnabled || _queue.Count == 0)
            return;

        var softMax = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var online = ActualPlayersCount;

        if (online >= softMax)
            return;

        var next = _queue.First();
        _queue.Remove(next);

        SendToGame(next);

        SendQueueUpdates();
        QueueCount.Set(_queue.Count);
    }

    private void SendQueueUpdates()
    {
        for (var i = 0; i < _queue.Count; i++)
        {
            _queue[i].Channel.SendMessage(new MsgQueueUpdate
            {
                Total = _queue.Count,
                Position = i + 1
            });
        }
    }

    private void SendToGame(ICommonSession session)
    {
        Timer.Spawn(0, () => _playerManager.JoinGame(session));
    }
}
