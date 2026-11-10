using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.ADT.CCVar;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.ADT.MapRotation;

public sealed class MapRotationManager
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IPlayerManager _players = default!;

    private const int PlayedHistoryLength = 10;
    public const int MaxBonusVotes = ushort.MaxValue / 2;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private int _excludeDepth;
    private int _bonusCap;
    private int _minPlayers;
    private string _server = string.Empty;

    private bool _loaded;

    private readonly Dictionary<string, double> _bonus = new();
    private readonly Dictionary<string, DateTime> _lastPlayed = new();

    private Dictionary<string, double>? _pendingTally;

    private Task _writeTask = Task.CompletedTask;

    public bool Loaded => _loaded;

    public int MinPlayers => _minPlayers;

    public IReadOnlyDictionary<string, double> Bonuses => _bonus;

    public bool Active => _enabled && _loaded && _players.PlayerCount >= _minPlayers;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("maprotation");

        _cfg.OnValueChanged(ADTCCVars.MapRotationEnabled, value => _enabled = value, true);
        _cfg.OnValueChanged(ADTCCVars.MapRotationExcludeDepth, value => _excludeDepth = Math.Max(0, value), true);
        _cfg.OnValueChanged(ADTCCVars.MapRotationBonusCap, value => _bonusCap = Math.Clamp(value, 0, MaxBonusVotes), true);
        _cfg.OnValueChanged(ADTCCVars.MapRotationMinPlayers, value => _minPlayers = Math.Max(0, value), true);

        _server = _cfg.GetCVar(CCVars.AdminLogsServerName);

        Load();
    }

    private async void Load()
    {
        try
        {
            var records = await _db.GetMapRotationAsync(_server);

            foreach (var record in records)
            {
                if (record.BonusPerPlayer > 0 && double.IsFinite(record.BonusPerPlayer))
                    _bonus[record.MapId] = record.BonusPerPlayer;

                if (record.LastPlayed is { } played)
                    _lastPlayed[record.MapId] = played;
            }

            _loaded = true;
            _sawmill.Info($"Loaded rotation for server {_server}: {_bonus.Count} maps with bonus votes, recent maps: {string.Join(", ", GetRecentMaps())}.");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load map rotation, it stays off until restart: {e}");
        }
    }

    public List<string> GetRecentMaps()
    {
        return _lastPlayed
            .OrderByDescending(p => p.Value)
            .Select(p => p.Key)
            .ToList();
    }

    public bool IsExcluded(string mapId)
    {
        if (!Active || _excludeDepth == 0)
            return false;

        return _lastPlayed
            .OrderByDescending(p => p.Value)
            .Take(_excludeDepth)
            .Any(p => p.Key == mapId);
    }

    public int GetBonus(string mapId)
    {
        if (!Active)
            return 0;

        return ToVotes(_bonus.GetValueOrDefault(mapId), _players.PlayerCount);
    }

    public int ToVotes(double perPlayer, int players)
    {
        var raw = perPlayer * players;
        if (double.IsNaN(raw) || raw <= 0)
            return 0;

        var limit = _bonusCap > 0 ? _bonusCap : MaxBonusVotes;
        return (int) Math.Min(Math.Round(raw), limit);
    }

    public void RecordVote(Dictionary<string, int> tally)
    {
        if (!Active)
        {
            _pendingTally = null;
            return;
        }

        var players = Math.Max(1, _players.PlayerCount);

        _pendingTally = tally.ToDictionary(p => p.Key, p => (double) Math.Clamp(p.Value, 0, MaxBonusVotes) / players);
    }

    public void RoundStarted(string mapId)
    {
        var tally = _pendingTally;
        _pendingTally = null;

        if (!_enabled)
            return;

        if (!_loaded)
        {
            _sawmill.Warning($"Round started on {mapId} before the rotation was loaded, skipping it.");
            return;
        }

        if (_players.PlayerCount < _minPlayers)
        {
            _sawmill.Info($"Round started on {mapId} with {_players.PlayerCount} players, below {_minPlayers}, rotation is not changed.");
            return;
        }

        if (tally != null)
        {
            foreach (var (map, perPlayer) in tally)
            {
                if (map == mapId)
                    continue;

                if (perPlayer > 0)
                    _bonus[map] = perPlayer;
                else
                    _bonus.Remove(map);
            }
        }

        _bonus.Remove(mapId);
        _lastPlayed[mapId] = DateTime.UtcNow;

        foreach (var old in GetRecentMaps().Skip(Math.Max(PlayedHistoryLength, _excludeDepth)))
        {
            _lastPlayed.Remove(old);
        }

        _sawmill.Info($"Round started on {mapId}. Bonus votes per player: {string.Join(", ", _bonus.Select(p => $"{p.Key}={p.Value:F3}"))}.");

        Save();
    }

    public void Reset()
    {
        _bonus.Clear();
        _lastPlayed.Clear();
        _pendingTally = null;

        if (_loaded)
            Save();
    }

    public void SetBonus(string mapId, int votes)
    {
        var players = Math.Max(1, Math.Max(_players.PlayerCount, _minPlayers));

        votes = Math.Min(votes, MaxBonusVotes);

        if (votes > 0)
            _bonus[mapId] = (double) votes / players;
        else
            _bonus.Remove(mapId);

        if (_loaded)
            Save();
    }

    private void Save()
    {
        var records = new List<MapRotationRecord>();

        foreach (var map in _bonus.Keys.Union(_lastPlayed.Keys))
        {
            DateTime? played = null;

            if (_lastPlayed.TryGetValue(map, out var time))
                played = time;

            records.Add(new MapRotationRecord(map, _bonus.GetValueOrDefault(map), played));
        }

        _writeTask = ChainSave(_writeTask, records);
    }

    private async Task ChainSave(Task previous, List<MapRotationRecord> records)
    {
        await previous;

        try
        {
            await _db.SaveMapRotationAsync(_server, records);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save map rotation: {e}");
        }
    }
}
