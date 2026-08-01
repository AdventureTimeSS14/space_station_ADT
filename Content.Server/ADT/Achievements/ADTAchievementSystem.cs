using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared.ADT.Achievements;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Achievements;

public sealed partial class ADTAchievementSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(300);

    private readonly Dictionary<string, List<ADTAchievementPrototype>> _byTrigger = new();

    private readonly Dictionary<NetUserId, PlayerAchievements> _progress = new();

    private Task _flushTask = Task.CompletedTask;
    private TimeSpan _nextFlush;

    private sealed class PlayerAchievements
    {
        public readonly Dictionary<string, ADTAchievementState> States = new();

        public readonly HashSet<string> Dirty = new();

        public bool Loaded;

        public readonly List<DeferredEvent> Deferred = new();
    }

    private readonly record struct DeferredEvent(string Trigger, EntityUid? Target, string? Key, int Amount);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTAchievementsRequestEvent>(OnStateRequest);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        BuildIndex();
        InitializeHooks();

        _nextFlush = _timing.CurTime + FlushInterval;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;

        Flush();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ADTAchievementPrototype>())
            BuildIndex();
    }

    private void BuildIndex()
    {
        _byTrigger.Clear();

        foreach (var achievement in _prototype.EnumeratePrototypes<ADTAchievementPrototype>())
        {
            foreach (var condition in achievement.Conditions)
            {
                foreach (var trigger in condition.Triggers)
                {
                    if (!_byTrigger.TryGetValue(trigger.Id, out var list))
                    {
                        list = new List<ADTAchievementPrototype>();
                        _byTrigger[trigger.Id] = list;
                    }

                    if (!list.Contains(achievement))
                        list.Add(achievement);
                }
            }
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                BeginLoad(args.Session);
                break;
            case SessionStatus.Disconnected:
                Unload(args.Session);
                break;
        }
    }

    private void BeginLoad(ICommonSession session)
    {
        if (_progress.ContainsKey(session.UserId))
            return;

        _progress[session.UserId] = new PlayerAchievements();

        _ = LoadAsync(session.UserId);
    }

    private async Task LoadAsync(NetUserId userId)
    {
        List<ADTAchievementRow> rows;

        try
        {
            rows = await _db.GetAchievementsAsync(userId.UserId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load achievements for {userId}: {e}");
            return;
        }

        if (!_progress.TryGetValue(userId, out var data) || data.Loaded)
            return;

        foreach (var row in rows)
        {
            data.States[row.AchievementId] = new ADTAchievementState
            {
                Progress = row.Progress,
                Unlocked = row.Unlocked,
            };
        }

        data.Loaded = true;

        if (data.Deferred.Count > 0)
        {
            var deferred = data.Deferred.ToArray();
            data.Deferred.Clear();

            foreach (var entry in deferred)
            {
                Progress(userId, entry.Trigger, entry.Target, entry.Key, entry.Amount);
            }
        }

        if (_playerManager.TryGetSessionById(userId, out var session))
            SendFullState(session, data);
    }

    private void Unload(ICommonSession session)
    {
        if (!_progress.Remove(session.UserId, out var data))
            return;

        var batch = new List<ADTAchievementSave>();
        CollectSaves(session.UserId, data, batch);

        if (batch.Count > 0)
            _flushTask = ChainFlush(_flushTask, batch);
    }

    public void Raise(
        ICommonSession? player,
        ProtoId<ADTAchievementTriggerPrototype> trigger,
        EntityUid? target = null,
        string? key = null,
        int amount = 1)
    {
        Raise(player?.UserId, trigger, target, key, amount);
    }

    public void Raise(
        NetUserId? player,
        ProtoId<ADTAchievementTriggerPrototype> trigger,
        EntityUid? target = null,
        string? key = null,
        int amount = 1)
    {
        if (player is not { } userId || amount <= 0)
            return;

        if (!_byTrigger.ContainsKey(trigger.Id))
            return;

        if (!_progress.TryGetValue(userId, out var data))
            return;

        if (!data.Loaded)
        {
            data.Deferred.Add(new DeferredEvent(trigger.Id, target, key, amount));
            return;
        }

        Progress(userId, trigger.Id, target, key, amount);
    }

    private void Progress(NetUserId userId, string trigger, EntityUid? target, string? key, int amount)
    {
        if (!_byTrigger.TryGetValue(trigger, out var candidates))
            return;

        if (!_progress.TryGetValue(userId, out var data))
            return;

        var args = new ADTAchievementConditionArgs(trigger, target, key, amount, EntityManager, _whitelist);

        _playerManager.TryGetSessionById(userId, out var session);

        foreach (var achievement in candidates)
        {
            data.States.TryGetValue(achievement.ID, out var state);

            if (state.Unlocked)
                continue;

            var gained = 0;

            foreach (var condition in achievement.Conditions)
            {
                if (!ConditionListens(condition, trigger))
                    continue;

                gained += condition.GetProgress(in args);
            }

            if (gained <= 0)
                continue;

            state.Progress += gained;

            var unlocked = state.Progress >= achievement.Goal;

            if (unlocked)
            {
                state.Progress = achievement.Goal;
                state.Unlocked = true;
            }

            data.States[achievement.ID] = state;
            data.Dirty.Add(achievement.ID);

            if (session != null)
            {
                RaiseNetworkEvent(new ADTAchievementUpdateEvent(achievement.ID, state, unlocked), session);

                if (unlocked)
                    AnnounceUnlock(session, achievement);
            }

            if (unlocked)
                Progress(userId, SharedADTAchievements.UnlockedTrigger, null, achievement.ID, 1);
        }
    }

    private void AnnounceUnlock(ICommonSession session, ADTAchievementPrototype achievement)
    {
        var message = Loc.GetString("adt-achievements-unlocked-message",
            ("name", Loc.GetString(achievement.Name)),
            ("description", Loc.GetString(achievement.Description)));

        _chat.ChatMessageToOne(
            ChatChannel.Server,
            message,
            message,
            EntityUid.Invalid,
            false,
            session.Channel,
            Color.Gold);
    }

    private static bool ConditionListens(ADTAchievementCondition condition, string trigger)
    {
        foreach (var listened in condition.Triggers)
        {
            if (listened.Id == trigger)
                return true;
        }

        return false;
    }

    public bool IsUnlocked(NetUserId userId, ProtoId<ADTAchievementPrototype> achievement)
    {
        return _progress.TryGetValue(userId, out var data)
               && data.States.TryGetValue(achievement.Id, out var state)
               && state.Unlocked;
    }

    private void OnStateRequest(ADTAchievementsRequestEvent args, EntitySessionEventArgs session)
    {
        if (!_progress.TryGetValue(session.SenderSession.UserId, out var data) || !data.Loaded)
            return;

        SendFullState(session.SenderSession, data);
    }

    private void SendFullState(ICommonSession session, PlayerAchievements data)
    {
        var payload = new Dictionary<string, ADTAchievementState>(data.States);
        RaiseNetworkEvent(new ADTAchievementsStateEvent(payload), session);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextFlush)
            return;

        _nextFlush = _timing.CurTime + FlushInterval;
        Flush();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Flush();
    }

    private void Flush()
    {
        var batch = new List<ADTAchievementSave>();

        foreach (var (userId, data) in _progress)
        {
            CollectSaves(userId, data, batch);
        }

        if (batch.Count == 0)
            return;

        _flushTask = ChainFlush(_flushTask, batch);
    }

    private static void CollectSaves(NetUserId userId, PlayerAchievements data, List<ADTAchievementSave> batch)
    {
        if (data.Dirty.Count == 0)
            return;

        foreach (var id in data.Dirty)
        {
            if (!data.States.TryGetValue(id, out var state))
                continue;

            batch.Add(new ADTAchievementSave
            {
                UserId = userId.UserId,
                AchievementId = id,
                Progress = state.Progress,
                Unlocked = state.Unlocked,
            });
        }

        data.Dirty.Clear();
    }

    private async Task ChainFlush(Task previous, List<ADTAchievementSave> batch)
    {
        try
        {
            await previous;
        }
        catch
        {
        }

        try
        {
            await _db.SaveAchievementsAsync(batch);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save {batch.Count} achievement rows: {e}");
        }
    }

}
