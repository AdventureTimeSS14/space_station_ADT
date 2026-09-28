using System.Linq;
using System.Threading;
using Content.Server.Administration.Managers;
using Content.Server.Corvax.JoinQueue;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.ADT.Administration.PlayersDashboard;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.Administration.PlayersDashboard;

public sealed partial class PlayersDashboardEui : BaseEui
{
    private static readonly TimeSpan RefreshTime = TimeSpan.FromSeconds(2);

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private JoinQueueManager _joinQueue = default!;
    [Dependency] private IAdminManager _adminManager = default!;

    private readonly MindSystem _mind;
    private readonly CancellationTokenSource _refreshToken = new();

    public PlayersDashboardEui()
    {
        IoCManager.InjectDependencies(this);
        _mind = _entityManager.System<MindSystem>();
    }

    public override void Opened()
    {
        base.Opened();

        StateDirty();
        Timer.SpawnRepeating(RefreshTime, StateDirty, _refreshToken.Token);
    }

    public override void Closed()
    {
        base.Closed();

        _refreshToken.Cancel();
    }

    public override EuiStateBase GetNewState()
    {
        var state = new PlayersDashboardEuiState
        {
            Total = _playerManager.PlayerCount,
            Queue = _joinQueue.PlayersInQueue,
            Admins = _adminManager.ActiveAdmins.Count(),
        };

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status != SessionStatus.InGame)
                continue;

            if (session.AttachedEntity is not { } entity)
            {
                state.Lobby++;
                continue;
            }

            if (!_mind.TryGetMind(session.UserId, out _, out var mind) || mind.CurrentEntity != entity)
                continue;

            if (_entityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID == GameTicker.AdminObserverPrototypeName.Id)
                state.AdminGhosts++;
            else if (_entityManager.HasComponent<GhostComponent>(entity))
                state.Ghosts++;
            else
                state.InGame++;
        }

        state.Other = state.Total - state.Queue - state.Lobby - state.Ghosts - state.AdminGhosts - state.InGame;
        return state;
    }
}
