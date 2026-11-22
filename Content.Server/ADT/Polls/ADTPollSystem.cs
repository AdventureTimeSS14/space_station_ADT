using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.ADT.Polls;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Polls;

public sealed partial class ADTPollSystem : EntitySystem
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedRoleSystem _roles = default!;

    private readonly Dictionary<int, PollData> _active = new();
    private readonly Dictionary<NetUserId, PollSessionState> _sessions = new();
    private readonly Dictionary<NetUserId, PollUserState> _loaded = new();

    private bool _enabled;
    private int _textMaxLength;
    private string _serverName = string.Empty;

    private sealed class PollSessionState
    {
        public readonly HashSet<int> Answered = new();
        public readonly Dictionary<int, PollSnapshot> Targeted = new();
        public bool Submitting;
    }

    public string ServerName => _serverName;

    public override void Initialize()
    {
        base.Initialize();

        InitializeParticipation();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeNetworkEvent<PollRequestEvent>(OnPollRequest);
        SubscribeNetworkEvent<PollSubmitEvent>(OnPollSubmit);

        Subs.CVar(_cfg, PollCVars.Enabled, v => _enabled = v, true);
        Subs.CVar(_cfg, PollCVars.TextMaxLength, v => _textMaxLength = v, true);
        Subs.CVar(_cfg, CCVars.AdminLogsServerName, v => _serverName = v, true);

        _userDb.AddOnLoadPlayer(LoadPlayer);
        _userDb.AddOnFinishLoad(FinishLoadPlayer);
        _userDb.AddOnPlayerDisconnect(OnPlayerDisconnect);

        StartupDatabase();
    }

    private async void StartupDatabase()
    {
        try
        {
            var days = _cfg.GetCVar(PollCVars.RetentionDays);

            if (days > 0)
            {
                var removed = await _db.CleanupPollsAsync(DateTime.UtcNow - TimeSpan.FromDays(days));

                if (removed > 0)
                    Log.Info($"Удалено старых опросов: {removed}.");
            }

            await ReloadActive();
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось загрузить опросы: {ex}");
        }
    }

    private async Task ReloadActive()
    {
        var polls = await _db.GetActivePollsAsync();
        var appeared = new List<PollData>();

        foreach (var poll in polls)
        {
            if (poll.ShowMode == PollShowMode.Immediately && !_active.ContainsKey(poll.Id))
                appeared.Add(poll);
        }

        _active.Clear();

        foreach (var poll in polls)
        {
            if (poll.RunsOn(_serverName))
                _active[poll.Id] = poll;
        }

        SendListToAll();

        if (!_enabled)
            return;

        foreach (var poll in appeared)
        {
            if (_active.ContainsKey(poll.Id))
                TargetAllOnline(poll);
        }
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        ReloadActiveSafe();
    }

    private async void ReloadActiveSafe()
    {
        try
        {
            await ReloadActive();
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось обновить опросы: {ex}");
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _round.Clear();
    }

    private IEnumerable<PollData> GetActive()
    {
        var now = DateTime.UtcNow;
        List<int>? expired = null;

        foreach (var (id, poll) in _active)
        {
            if (poll.IsActive(now))
                continue;

            expired ??= new List<int>();
            expired.Add(id);
        }

        if (expired != null)
        {
            foreach (var id in expired)
            {
                _active.Remove(id);
            }
        }

        return _active.Values;
    }

    private bool TryGetActive(int pollId, out PollData poll)
    {
        if (_active.TryGetValue(pollId, out poll!) && poll.IsActive(DateTime.UtcNow))
            return true;

        _active.Remove(pollId);
        return false;
    }

    #region Сессии

    private async Task LoadPlayer(ICommonSession session, CancellationToken cancel)
    {
        try
        {
            var state = await _db.GetPollUserStateAsync(session.UserId.UserId);
            cancel.ThrowIfCancellationRequested();
            _loaded[session.UserId] = state;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось загрузить опросы игрока {session}: {ex}");
        }
    }

    private void FinishLoadPlayer(ICommonSession session)
    {
        var state = new PollSessionState();

        if (_loaded.Remove(session.UserId, out var loaded))
        {
            state.Answered.UnionWith(loaded.Answered);

            foreach (var (pollId, snapshot) in loaded.Pending)
            {
                state.Targeted[pollId] = snapshot;
            }
        }

        _sessions[session.UserId] = state;

        if (!_enabled)
            return;

        var newTargets = new List<PollData>();

        foreach (var poll in GetActive())
        {
            if (poll.ShowMode != PollShowMode.Immediately)
                continue;

            newTargets.Add(poll);
        }

        TargetSession(session, newTargets, false);
        SendList(session);
        SendInvites(session, state.Targeted.Keys.Where(id => !state.Answered.Contains(id) && _active.ContainsKey(id)).ToList());
    }

    private void OnPlayerDisconnect(ICommonSession session)
    {
        _sessions.Remove(session.UserId);
        _loaded.Remove(session.UserId);
    }

    #endregion

    #region Таргетинг

    private List<int> TargetSession(ICommonSession session, List<PollData> polls, bool invite)
    {
        var added = new List<int>();

        if (!_sessions.TryGetValue(session.UserId, out var state))
            return added;

        var pending = new List<PollPendingEntry>();
        PollSnapshot? snapshot = null;

        foreach (var poll in polls)
        {
            if (state.Answered.Contains(poll.Id) || state.Targeted.ContainsKey(poll.Id))
                continue;

            snapshot ??= BuildSnapshot(session);

            if (!PollTargeting.Matches(poll.Filters, snapshot))
                continue;

            state.Targeted[poll.Id] = snapshot;
            pending.Add(new PollPendingEntry(poll.Id, session.UserId.UserId, snapshot));
            added.Add(poll.Id);
        }

        if (added.Count == 0)
            return added;

        SavePending(pending);

        if (invite)
        {
            SendList(session);
            SendInvites(session, added);
        }

        return added;
    }

    private void TargetJoined(ICommonSession session)
    {
        if (!_enabled)
            return;

        var polls = GetActive()
            .Where(p => p.ShowMode is PollShowMode.RoundStart or PollShowMode.Immediately)
            .ToList();

        if (polls.Count > 0)
            TargetSession(session, polls, true);
    }

    private void TargetAllOnline(PollData poll)
    {
        var polls = new List<PollData>
        {
            poll,
        };

        foreach (var session in _players.Sessions)
        {
            TargetSession(session, polls, true);
        }
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        if (!_enabled)
            return;

        var polls = GetActive()
            .Where(p => p.ShowMode == PollShowMode.RoundEnd)
            .ToList();

        if (polls.Count == 0)
            return;

        CollectRoundMinds();

        var online = new HashSet<NetUserId>();

        foreach (var session in _players.Sessions)
        {
            if (!_sessions.ContainsKey(session.UserId))
                continue;

            online.Add(session.UserId);
            TargetSession(session, polls, true);
        }

        var offline = new List<PollPendingEntry>();

        foreach (var (userId, entry) in _round)
        {
            if (online.Contains(userId))
                continue;

            foreach (var poll in polls)
            {
                if (PollTargeting.Matches(poll.Filters, entry))
                    offline.Add(new PollPendingEntry(poll.Id, userId.UserId, entry.Clone()));
            }
        }

        SavePending(offline);
    }

    private async void SavePending(List<PollPendingEntry> entries)
    {
        if (entries.Count == 0)
            return;

        try
        {
            await _db.AddPollPendingAsync(entries);
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось сохранить адресатов опросов: {ex}");
        }
    }

    #endregion

    #region Сеть

    private void SendList(ICommonSession session)
    {
        if (!_sessions.TryGetValue(session.UserId, out var state))
            return;

        var ev = new PollListEvent();

        if (_enabled)
        {
            foreach (var poll in GetActive())
            {
                ev.Polls.Add(new PollSummary
                {
                    Id = poll.Id,
                    Title = poll.Title,
                    Targeted = state.Targeted.ContainsKey(poll.Id),
                    Answered = state.Answered.Contains(poll.Id),
                    EndsAt = poll.EndsAt,
                });
            }
        }

        RaiseNetworkEvent(ev, session);
    }

    private void SendListToAll()
    {
        foreach (var session in _players.Sessions)
        {
            SendList(session);
        }
    }

    private void SendInvites(ICommonSession session, List<int> pollIds)
    {
        if (pollIds.Count == 0)
            return;

        var ev = new PollInviteEvent
        {
            PollIds = pollIds,
        };

        RaiseNetworkEvent(ev, session);
    }

    private void OnPollRequest(PollRequestEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (!_enabled || !_sessions.TryGetValue(session.UserId, out var state))
            return;

        if (!TryGetActive(ev.PollId, out var poll))
        {
            SendList(session);
            return;
        }

        var content = new PollContentEvent
        {
            Poll = StripForPlayer(poll),
            Answered = state.Answered.Contains(poll.Id),
        };

        RaiseNetworkEvent(content, session);
    }

    private static PollData StripForPlayer(PollData poll)
    {
        return new PollData
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            Questions = poll.Questions,
            ShowMode = poll.ShowMode,
            CreatedAt = poll.CreatedAt,
            EndsAt = poll.EndsAt,
        };
    }

    private async void OnPollSubmit(PollSubmitEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (!_enabled || !_sessions.TryGetValue(session.UserId, out var state) || state.Submitting)
            return;

        if (!TryGetActive(ev.PollId, out var poll))
        {
            SendResult(session, ev.PollId, false, "adt-poll-error-closed");
            SendList(session);
            return;
        }

        if (state.Answered.Contains(poll.Id))
        {
            SendResult(session, poll.Id, false, "adt-poll-error-already-answered");
            return;
        }

        var error = PollValidation.ValidateAnswers(poll, ev.Answers, _textMaxLength, out var answers);

        if (error != null)
        {
            SendResult(session, poll.Id, false, error);
            return;
        }

        var targeted = state.Targeted.TryGetValue(poll.Id, out var snapshot);

        var response = new PollResponseView
        {
            UserId = session.UserId.UserId,
            RoundId = _ticker.RoundId == 0 ? null : _ticker.RoundId,
            Server = _serverName,
            Targeted = targeted,
            Snapshot = snapshot ?? BuildSnapshot(session),
            Answers = answers,
        };

        state.Submitting = true;

        try
        {
            var saved = await _db.SubmitPollResponseAsync(response, poll.Id);

            state.Answered.Add(poll.Id);

            if (saved)
                SendResult(session, poll.Id, true, "adt-poll-submit-success");
            else
                SendResult(session, poll.Id, false, "adt-poll-error-already-answered");
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось сохранить ответ {session} на опрос {poll.Id}: {ex}");
            SendResult(session, poll.Id, false, "adt-poll-error-save");
        }
        finally
        {
            state.Submitting = false;
        }

        if (session.Status != SessionStatus.Disconnected)
            SendList(session);
    }

    private void SendResult(ICommonSession session, int pollId, bool success, string message)
    {
        if (session.Status == SessionStatus.Disconnected)
            return;

        var ev = new PollSubmitResultEvent
        {
            PollId = pollId,
            Success = success,
            Message = message,
        };

        RaiseNetworkEvent(ev, session);
    }

    #endregion
}
