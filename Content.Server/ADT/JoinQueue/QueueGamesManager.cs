using System.Linq;
using System.Threading;
using Content.Server.Connection;
using Content.Server.Corvax.JoinQueue;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.JoinQueue;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.JoinQueue;

public sealed partial class QueueGamesManager
{
    private static readonly TimeSpan SpinTime = TimeSpan.FromSeconds(3.8);
    private static readonly TimeSpan WinBypassDelay = TimeSpan.FromSeconds(3);

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private JoinQueueManager _joinQueue = default!;
    [Dependency] private IConnectionManager _connectionManager = default!;

    private readonly Dictionary<ICommonSession, QueueGamesPlayer> _players = new();
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("queue.games");

        _net.RegisterNetMessage<MsgQueueGameAction>(OnGameAction);
        _net.RegisterNetMessage<MsgQueueGameState>();
        _net.RegisterNetMessage<MsgQueueSlotResult>();

        _joinQueue.PlayerJoinedQueue += OnPlayerJoinedQueue;
        _joinQueue.PlayerLeftQueue += OnPlayerLeftQueue;
    }

    private void OnPlayerJoinedQueue(ICommonSession session)
    {
        if (!_cfg.GetCVar(ADTCCVars.QueueGamesEnabled))
            return;

        _players[session] = new QueueGamesPlayer();
        SendState(session);
    }

    private void OnPlayerLeftQueue(ICommonSession session)
    {
        if (!_players.Remove(session, out var player) || player.Match is not { } match)
            return;

        match.TurnTimer?.Cancel();

        var opponent = match.GetOpponent(session);
        if (!_players.TryGetValue(opponent, out var opponentPlayer))
            return;

        opponentPlayer.Match = null;
        opponentPlayer.LastBoard = match.Board;
        opponentPlayer.LastResult = QueueGameResult.OpponentLeft;
        SendState(opponent);
    }

    private void OnGameAction(MsgQueueGameAction msg)
    {
        if (!_cfg.GetCVar(ADTCCVars.QueueGamesEnabled) ||
            !_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session) ||
            !_players.TryGetValue(session, out var player))
            return;

        switch (msg.Action)
        {
            case QueueGameAction.FindOpponent:
                FindOpponent(session, player);
                break;
            case QueueGameAction.CancelSearch:
                player.Searching = false;
                SendState(session);
                break;
            case QueueGameAction.MakeMove:
                MakeMove(session, player, msg.Cell);
                break;
            case QueueGameAction.SpinSlot:
                Spin(session, player);
                break;
        }
    }

    private void FindOpponent(ICommonSession session, QueueGamesPlayer player)
    {
        if (player.Searching || player.Spinning || player.Match != null)
            return;

        var opponents = _players.Where(p => p.Value.Searching).Select(p => p.Key).ToList();
        if (opponents.Count == 0)
        {
            player.Searching = true;
            SendState(session);
            return;
        }

        var opponent = _random.Pick(opponents);
        var match = _random.Prob(0.5f)
            ? new QueueTicTacToeMatch(session, opponent)
            : new QueueTicTacToeMatch(opponent, session);

        foreach (var matchPlayer in new[] { player, _players[opponent] })
        {
            matchPlayer.Searching = false;
            matchPlayer.Match = match;
            matchPlayer.LastResult = QueueGameResult.None;
        }

        StartTurn(match);
    }

    private void MakeMove(ICommonSession session, QueueGamesPlayer player, int cell)
    {
        if (player.Match is not { } match || !match.TryMove(session, cell))
            return;

        if (match.HasWinner())
        {
            EndMatch(match, session);
            return;
        }

        if (match.IsBoardFull())
        {
            EndMatch(match, null);
            return;
        }

        match.NextTurn();
        StartTurn(match);
    }

    private void StartTurn(QueueTicTacToeMatch match)
    {
        match.TurnTimer?.Cancel();
        match.TurnTimer = new CancellationTokenSource();

        var timedOut = match.CurrentPlayer;
        Timer.Spawn(TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.QueueGamesTurnTime)),
            () => EndMatch(match, match.GetOpponent(timedOut)),
            match.TurnTimer.Token);

        SendState(match.Cross);
        SendState(match.Nought);
    }

    private void EndMatch(QueueTicTacToeMatch match, ICommonSession? winner)
    {
        match.TurnTimer?.Cancel();

        foreach (var session in new[] { match.Cross, match.Nought })
        {
            if (!_players.TryGetValue(session, out var player) || player.Match != match)
                continue;

            player.Match = null;
            player.LastBoard = match.Board;

            if (winner == null)
            {
                player.LastResult = QueueGameResult.Draw;
            }
            else if (winner == session)
            {
                player.Score++;
                player.LastResult = QueueGameResult.Win;
            }
            else
            {
                player.Score = 0;
                player.LastResult = QueueGameResult.Lose;
            }

            SendState(session);
        }
    }

    private void Spin(ICommonSession session, QueueGamesPlayer player)
    {
        if (player.Spinning || player.Searching || player.Match != null ||
            player.Score < _cfg.GetCVar(ADTCCVars.QueueGamesScoreToSpin))
            return;

        player.Spinning = true;
        SendState(session);

        Timer.Spawn(SpinTime, () => FinishSpin(session, player));
    }

    private void FinishSpin(ICommonSession session, QueueGamesPlayer player)
    {
        if (!_players.TryGetValue(session, out var current) || current != player)
            return;

        var won = _random.Prob(_cfg.GetCVar(ADTCCVars.QueueGamesSlotWinChance));
        _net.ServerSendMessage(new MsgQueueSlotResult { Won = won }, session.Channel);

        if (!won)
        {
            player.Spinning = false;
            player.Score = 0;
            SendState(session);
            return;
        }

        _sawmill.Info($"{session} won the queue slot machine and skips the join queue");

        var bypassTime = TimeSpan.FromMinutes(_cfg.GetCVar(ADTCCVars.QueueGamesSlotBypassMinutes));
        _connectionManager.AddTemporaryConnectBypass(session.UserId, bypassTime);

        Timer.Spawn(WinBypassDelay, () => _joinQueue.TryBypassQueue(session));
    }

    private void SendState(ICommonSession session)
    {
        if (!_players.TryGetValue(session, out var player))
            return;

        var match = player.Match;
        var msg = new MsgQueueGameState
        {
            Score = player.Score,
            ScoreToSpin = _cfg.GetCVar(ADTCCVars.QueueGamesScoreToSpin),
            Searching = player.Searching,
            Spinning = player.Spinning,
            InMatch = match != null,
            OpponentName = match?.GetOpponent(session).Name ?? string.Empty,
            Mark = match?.GetMark(session) ?? QueueGameMark.None,
            YourTurn = match?.CurrentPlayer == session,
            LastResult = player.LastResult,
        };

        (match?.Board ?? player.LastBoard)?.CopyTo(msg.Board, 0);

        _net.ServerSendMessage(msg, session.Channel);
    }

    private sealed class QueueGamesPlayer
    {
        public int Score;
        public bool Searching;
        public bool Spinning;
        public QueueTicTacToeMatch? Match;
        public QueueGameResult LastResult;
        public QueueGameMark[]? LastBoard;
    }
}
