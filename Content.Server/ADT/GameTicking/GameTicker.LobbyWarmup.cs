using Content.Server.ADT.MapRotation;
using Content.Shared.ADT.CCVar;
using Content.Shared.Voting;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private MapRotationManager _mapRotation = default!;

    [ViewVariables]
    private bool _lobbyWarmup;

    [ViewVariables]
    private TimeSpan? _lobbyWarmupEnd;

    private int _lobbyWarmupGeneration;

    private int _lobbyWarmupJoin;

    public bool LobbyWarmup => _lobbyWarmup;

    private void StartLobbyAutoVotes()
    {
        if (!_cfg.GetCVar(ADTCCVars.LobbyAutoVote))
            return;

        _voteManager.CreateStandardVote(initiator: null, voteType: StandardVoteType.Map);
        _voteManager.CreateStandardVote(initiator: null, voteType: StandardVoteType.Preset);
    }

    private void BeginLobby(bool noPlayers)
    {
        _lobbyWarmup = false;
        _lobbyWarmupEnd = null;
        _lobbyWarmupGeneration++;

        if (noPlayers && _cfg.GetCVar(ADTCCVars.LobbyWarmupEnabled))
        {
            _lobbyWarmup = true;
            return;
        }

        StartLobbyAutoVotes();
    }

    private bool HandleLobbyWarmupConnect()
    {
        if (!_lobbyWarmup)
            return false;

        var minPlayers = _cfg.GetCVar(ADTCCVars.LobbyWarmupMinPlayers);

        if (_playerManager.PlayerCount >= minPlayers)
        {
            EndLobbyWarmup();
            return true;
        }

        if (_lobbyWarmupEnd == null)
        {
            var wait = TimeSpan.FromSeconds(Math.Max(0, _cfg.GetCVar(ADTCCVars.LobbyWarmupMaxWait)));
            var generation = _lobbyWarmupGeneration;

            _lobbyWarmupEnd = _gameTiming.CurTime + wait;

            Robust.Shared.Timing.Timer.Spawn(wait, () =>
            {
                if (generation == _lobbyWarmupGeneration)
                    EndLobbyWarmup();
            });
        }

        var idle = TimeSpan.FromSeconds(Math.Max(1, _cfg.GetCVar(ADTCCVars.LobbyWarmupIdle)));
        var idleGeneration = _lobbyWarmupGeneration;
        var join = ++_lobbyWarmupJoin;

        Robust.Shared.Timing.Timer.Spawn(idle, () =>
        {
            if (idleGeneration == _lobbyWarmupGeneration && join == _lobbyWarmupJoin)
                EndLobbyWarmup();
        });

        return true;
    }

    public bool EndLobbyWarmup()
    {
        if (!_lobbyWarmup)
            return false;

        _lobbyWarmup = false;
        _lobbyWarmupEnd = null;
        _lobbyWarmupGeneration++;

        if (RunLevel != GameRunLevel.PreRoundLobby)
            return false;

        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
        _roundStartTime = _gameTiming.CurTime + LobbyDuration;

        SendStatusToAll();
        UpdateInfoText();
        StartLobbyAutoVotes();
        return true;
    }
}
