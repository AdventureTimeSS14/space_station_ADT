using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.ADT.Thunderdome;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Thunderdome;

public sealed class ThunderdomeStatsSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private sealed class RoundRecord
    {
        public int Kills;
        public int Deaths;
        public int BestStreak;
        public float Score;
        public int DiscardedKills;

        public readonly Dictionary<NetUserId, int> VictimKills = new();
    }

    private readonly Dictionary<NetUserId, RoundRecord> _round = new();
    private readonly Dictionary<NetUserId, ThunderdomeStatsDelta> _pending = new();
    private readonly Dictionary<NetUserId, TimeSpan> _requestCooldown = new();
    private readonly Dictionary<NetUserId, (ThunderdomePersonalStats? Stats, TimeSpan Expiry)> _personalCache = new();

    private List<ThunderdomeLeaderboardRow> _cachedTop = new();
    private TimeSpan _cacheExpiry;
    private Task<List<ThunderdomeLeaderboardRow>>? _topFetch;

    private Task _flushTask = Task.CompletedTask;

    private TimeSpan _nextFlush;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ThunderdomeLeaderboardRequestEvent>(OnLeaderboardRequest);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;

        _nextFlush = NextFlushTime();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextFlush)
            return;

        _nextFlush = NextFlushTime();
        Flush();
    }

    private TimeSpan NextFlushTime()
    {
        return _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(ThunderdomeCVars.StatsFlushInterval));
    }

    private bool Enabled
    {
        get { return _cfg.GetCVar(ThunderdomeCVars.StatsEnabled); }
    }

    public void RegisterSpawn(NetUserId user)
    {
        if (!Enabled)
            return;

        GetRound(user);
    }

    public void RegisterDeath(
        NetUserId victim,
        NetUserId? killer,
        TimeSpan lifetime,
        int arenaPlayers,
        int killerStreak)
    {
        if (!Enabled)
            return;

        GetRound(victim).Deaths++;
        AddPending(victim).Deaths++;

        if (killer is not { } killerUser || killerUser == victim)
            return;

        var killerRecord = GetRound(killerUser);
        var weight = ScoreKill(killerRecord, victim, lifetime, arenaPlayers);

        killerRecord.VictimKills.TryGetValue(victim, out var repeats);
        killerRecord.VictimKills[victim] = repeats + 1;

        if (killerStreak > killerRecord.BestStreak)
            killerRecord.BestStreak = killerStreak;

        if (weight <= 0f)
        {
            killerRecord.DiscardedKills++;
            return;
        }

        killerRecord.Kills++;
        killerRecord.Score += weight;

        var pending = AddPending(killerUser);
        pending.Kills++;
        pending.Score += weight;
        pending.BestStreak = Math.Max(pending.BestStreak, killerRecord.BestStreak);
    }

    private float ScoreKill(RoundRecord killer, NetUserId victim, TimeSpan lifetime, int arenaPlayers)
    {
        if (arenaPlayers < _cfg.GetCVar(ThunderdomeCVars.StatsMinPlayers))
            return 0f;

        if (lifetime.TotalSeconds < _cfg.GetCVar(ThunderdomeCVars.StatsMinLifetime))
            return 0f;

        killer.VictimKills.TryGetValue(victim, out var repeats);

        var free = Math.Max(0, _cfg.GetCVar(ThunderdomeCVars.StatsRepeatFree));
        var weight = 1f;

        if (repeats >= free)
        {
            var decay = Math.Clamp(_cfg.GetCVar(ThunderdomeCVars.StatsRepeatDecay), 0f, 0.99f);
            weight = MathF.Pow(decay, repeats - free + 1);
        }

        if (weight < _cfg.GetCVar(ThunderdomeCVars.StatsMinWeight))
            return 0f;

        return weight;
    }

    private RoundRecord GetRound(NetUserId user)
    {
        if (!_round.TryGetValue(user, out var record))
        {
            record = new RoundRecord();
            _round[user] = record;
        }

        return record;
    }

    private ThunderdomeStatsDelta AddPending(NetUserId user)
    {
        if (!_pending.TryGetValue(user, out var delta))
        {
            delta = new ThunderdomeStatsDelta
            {
                UserId = user.UserId,
            };
            _pending[user] = delta;
        }

        _personalCache.Remove(user);
        return delta;
    }

    private void Flush()
    {
        if (!Enabled || _pending.Count == 0)
            return;

        var batch = new Dictionary<NetUserId, ThunderdomeStatsDelta>(_pending);
        _pending.Clear();

        _flushTask = ChainFlush(_flushTask, batch);
    }

    private async Task ChainFlush(Task previous, Dictionary<NetUserId, ThunderdomeStatsDelta> batch)
    {
        await previous;
        await FlushBatch(batch);
    }

    private async Task FlushBatch(Dictionary<NetUserId, ThunderdomeStatsDelta> batch)
    {
        var payload = batch.Values.Where(d => !d.IsEmpty).ToList();
        if (payload.Count == 0)
            return;

        try
        {
            await _db.SaveThunderdomeStatsAsync(payload);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save thunderdome stats for {payload.Count} player(s), keeping them buffered: {e}");

            foreach (var (user, delta) in batch)
            {
                MergeBack(user, delta);
            }
        }
    }

    private void MergeBack(NetUserId user, ThunderdomeStatsDelta delta)
    {
        if (!_pending.TryGetValue(user, out var current))
        {
            _pending[user] = delta;
            return;
        }

        current.Kills += delta.Kills;
        current.Deaths += delta.Deaths;
        current.Score += delta.Score;
        current.RoundsPlayed += delta.RoundsPlayed;
        current.BestStreak = Math.Max(current.BestStreak, delta.BestStreak);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected)
            return;

        _requestCooldown.Remove(args.Session.UserId);
        _personalCache.Remove(args.Session.UserId);

        if (_pending.ContainsKey(args.Session.UserId))
            Flush();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!Enabled || _round.Count == 0)
            return;

        AppendRoundSummary(ev);

        foreach (var user in _round.Keys)
        {
            AddPending(user).RoundsPlayed++;
        }

        SendRoundReports();
    }

    private async void SendRoundReports()
    {
        var recipients = _players.Sessions
            .Where(s => _round.ContainsKey(s.UserId))
            .ToList();

        var roundStats = new Dictionary<NetUserId, ThunderdomeRoundStats?>();
        foreach (var session in recipients)
        {
            roundStats[session.UserId] = BuildRoundStats(session.UserId);
        }

        Flush();

        try
        {
            await _flushTask;
        }
        catch (Exception e)
        {
            Log.Error($"Thunderdome round end flush failed: {e}");
        }

        foreach (var session in recipients)
        {
            SendLeaderboard(session, roundStats.GetValueOrDefault(session.UserId));
        }
    }

    private void AppendRoundSummary(RoundEndTextAppendEvent ev)
    {
        var ranked = RankRound();
        if (ranked.Count == 0)
            return;

        ev.AddLine(string.Empty);
        ev.AddLine(Loc.GetString("thunderdome-roundend-header"));

        var shown = Math.Min(3, ranked.Count);
        for (var i = 0; i < shown; i++)
        {
            var (user, record) = ranked[i];
            var name = _players.TryGetPlayerData(user, out var data)
                ? data.UserName
                : Loc.GetString("thunderdome-unknown-player");

            ev.AddLine(Loc.GetString("thunderdome-roundend-entry",
                ("rank", i + 1),
                ("name", name),
                ("score", MathF.Round(record.Score, 1)),
                ("kills", record.Kills),
                ("deaths", record.Deaths)));
        }
    }

    private List<KeyValuePair<NetUserId, RoundRecord>> RankRound()
    {
        return _round
            .Where(p => p.Value.Kills > 0 || p.Value.Deaths > 0)
            .OrderByDescending(p => p.Value.Score)
            .ThenByDescending(p => p.Value.Kills)
            .ToList();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Flush();

        _round.Clear();
        _requestCooldown.Clear();
        _personalCache.Clear();
        _cachedTop = new List<ThunderdomeLeaderboardRow>();
        _cacheExpiry = TimeSpan.Zero;
    }

    private void OnLeaderboardRequest(ThunderdomeLeaderboardRequestEvent ev, EntitySessionEventArgs args)
    {
        RequestLeaderboard(args.SenderSession);
    }

    public void RequestLeaderboard(ICommonSession session)
    {
        if (!Enabled)
            return;

        var now = _timing.CurTime;
        if (_requestCooldown.TryGetValue(session.UserId, out var readyAt) && now < readyAt)
            return;

        _requestCooldown[session.UserId] = now + TimeSpan.FromSeconds(_cfg.GetCVar(ThunderdomeCVars.StatsRequestCooldown));

        SendLeaderboard(session, BuildRoundStats(session.UserId));
    }

    private async void SendLeaderboard(ICommonSession session, ThunderdomeRoundStats? round)
    {
        try
        {
            var top = await GetTop();
            var personal = await GetPersonal(session.UserId);

            if (session.Status == SessionStatus.Disconnected)
                return;

            var rows = new List<ThunderdomeLeaderboardEntry>(top.Count);
            for (var i = 0; i < top.Count; i++)
            {
                var row = top[i];
                rows.Add(new ThunderdomeLeaderboardEntry
                {
                    Rank = i + 1,
                    Name = row.Name,
                    Score = row.Score,
                    Kills = row.Kills,
                    Deaths = row.Deaths,
                    BestStreak = row.BestStreak,
                    IsSelf = row.UserId == session.UserId.UserId,
                });
            }

            RaiseNetworkEvent(new ThunderdomeLeaderboardEvent(rows, personal, round), session.Channel);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to build the thunderdome leaderboard for {session.Name}: {e}");
        }
    }

    private ThunderdomeRoundStats? BuildRoundStats(NetUserId user)
    {
        if (!_round.TryGetValue(user, out var record))
            return null;

        var ranked = RankRound();

        return new ThunderdomeRoundStats
        {
            Kills = record.Kills,
            Deaths = record.Deaths,
            BestStreak = record.BestStreak,
            Score = record.Score,
            Rank = ranked.FindIndex(p => p.Key == user) + 1,
            Participants = ranked.Count,
            DiscardedKills = record.DiscardedKills,
        };
    }

    private Task<List<ThunderdomeLeaderboardRow>> GetTop()
    {
        if (_timing.CurTime < _cacheExpiry)
            return Task.FromResult(_cachedTop);

        _topFetch ??= FetchTop();
        return _topFetch;
    }

    private async Task<List<ThunderdomeLeaderboardRow>> FetchTop()
    {
        try
        {
            await _flushTask;

            var size = Math.Clamp(_cfg.GetCVar(ThunderdomeCVars.StatsLeaderboardSize), 1, 50);
            _cachedTop = await _db.GetThunderdomeLeaderboardAsync(size);
            _cacheExpiry = _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(ThunderdomeCVars.StatsCacheTtl));

            return _cachedTop;
        }
        finally
        {
            _topFetch = null;
        }
    }

    private async Task<ThunderdomePersonalStats> GetPersonal(NetUserId user)
    {
        if (!_personalCache.TryGetValue(user, out var cached) || _timing.CurTime >= cached.Expiry)
        {
            await _flushTask;

            var fetched = await _db.GetThunderdomeStatsAsync(user.UserId);
            cached = (fetched, _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(ThunderdomeCVars.StatsCacheTtl)));
            _personalCache[user] = cached;
        }

        var stats = cached.Stats;
        var result = new ThunderdomePersonalStats
        {
            Rank = stats?.Rank ?? 0,
            TotalRanked = stats?.TotalRanked ?? 0,
            Score = stats?.Score ?? 0f,
            Kills = stats?.Kills ?? 0,
            Deaths = stats?.Deaths ?? 0,
            BestStreak = stats?.BestStreak ?? 0,
            RoundsPlayed = stats?.RoundsPlayed ?? 0,
        };

        if (_pending.TryGetValue(user, out var delta))
        {
            result.Kills += delta.Kills;
            result.Deaths += delta.Deaths;
            result.Score += delta.Score;
            result.RoundsPlayed += delta.RoundsPlayed;
            result.BestStreak = Math.Max(result.BestStreak, delta.BestStreak);
        }

        return result;
    }
}
