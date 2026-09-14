using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.ADT.CCVar;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Antag;

public sealed class AntagRollBonusManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public const int MaxMissedRounds = 20;

    private static readonly TimeSpan WipeRetryInterval = TimeSpan.FromMinutes(10);

    private const float MaxWipeHours = 24f * 365f;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private float _bonusPerRound;
    private float _wipeHours;

    private readonly Dictionary<NetUserId, Dictionary<ProtoId<AntagPrototype>, int>> _missedRounds = new();

    private readonly HashSet<NetUserId> _loaded = new();

    private readonly HashSet<(NetUserId User, ProtoId<AntagPrototype> Role)> _preSelected = new();

    private readonly HashSet<(NetUserId User, ProtoId<AntagPrototype> Role)> _becameAntag = new();

    private readonly HashSet<(NetUserId User, ProtoId<AntagPrototype> Role)> _eligible = new();

    private Task _writeTask = Task.CompletedTask;

    private int _generation;

    private DateTime? _lastWipe;
    private DateTime _nextWipe = DateTime.MaxValue;

    public bool Enabled => _enabled;

    public float BonusPerRound => _bonusPerRound;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("antag.rollbonus");

        _cfg.OnValueChanged(ADTCCVars.AntagRollBonusEnabled, OnEnabledChanged, true);
        _cfg.OnValueChanged(ADTCCVars.AntagRollBonusPerRound, OnBonusPerRoundChanged, true);
        _cfg.OnValueChanged(ADTCCVars.AntagRollBonusWipeHours, OnWipeHoursChanged, true);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;

        LoadWipeSchedule();

        _sawmill.Info($"Initialized. Enabled: {_enabled}, bonus per missed round: {_bonusPerRound:P0}, cap: {MaxMissedRounds} rounds.");
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;
        _sawmill.Info($"Roll bonus {(value ? "enabled" : "disabled")}.");
    }

    private void OnBonusPerRoundChanged(float value)
    {
        _bonusPerRound = value;
        _sawmill.Info($"Bonus per missed round set to {value:P0}. Max weight is now {1f + value * MaxMissedRounds:F2}.");
    }

    private void OnWipeHoursChanged(float value)
    {
        _wipeHours = value;
        RescheduleWipe();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                Load(args.Session.UserId);
                break;

            case SessionStatus.Disconnected:
                if (_players.TryGetSessionById(args.Session.UserId, out var current) && current != args.Session)
                    break;

                _missedRounds.Remove(args.Session.UserId);
                _loaded.Remove(args.Session.UserId);
                break;
        }
    }

    private async void Load(NetUserId user)
    {
        if (!_missedRounds.TryAdd(user, new Dictionary<ProtoId<AntagPrototype>, int>()))
            return;

        var generation = _generation;

        try
        {
            await _writeTask;
            var stored = await _db.GetAntagRollBonusAsync(user.UserId);

            if (!_missedRounds.TryGetValue(user, out var roles))
                return;

            if (generation == _generation)
            {
                foreach (var (antag, missed) in stored)
                {
                    roles[antag] = missed;
                }
            }

            _loaded.Add(user);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load the roll bonus of {user}: {e}");
        }
    }

    public float GetWeight(ICommonSession session, ProtoId<AntagPrototype> role)
    {
        if (!_enabled)
            return 1f;

        return GetWeight(GetMissedRounds(session.UserId, role));
    }

    public float GetWeight(int missedRounds)
    {
        return 1f + _bonusPerRound * missedRounds;
    }

    public int GetMissedRounds(NetUserId user, ProtoId<AntagPrototype> role)
    {
        if (!_missedRounds.TryGetValue(user, out var roles))
            return 0;

        return roles.GetValueOrDefault(role);
    }

    public Dictionary<ProtoId<AntagPrototype>, float> GetWeights(ICommonSession session, IEnumerable<ProtoId<AntagPrototype>> roles)
    {
        var result = new Dictionary<ProtoId<AntagPrototype>, float>();

        foreach (var role in roles)
        {
            result[role] = GetWeight(session, role);
        }

        return result;
    }

    public void MarkEligible(IEnumerable<ICommonSession> sessions, ProtoId<AntagPrototype> role)
    {
        if (!_enabled)
            return;

        foreach (var session in sessions)
        {
            _eligible.Add((session.UserId, role));
        }
    }

    public void MarkPreSelected(ICommonSession session, ProtoId<AntagPrototype> role)
    {
        if (!_enabled)
            return;

        _preSelected.Add((session.UserId, role));
    }

    public void MarkBecameAntag(ICommonSession session, ProtoId<AntagPrototype> role)
    {
        if (!_enabled)
            return;

        if (!_preSelected.Contains((session.UserId, role)))
            return;

        _becameAntag.Add((session.UserId, role));
    }

    public void FinishRound(IEnumerable<NetUserId> participants)
    {
        if (!_enabled)
        {
            ClearRoundState();
            return;
        }

        var played = new HashSet<NetUserId>(participants);

        var wasAntag = new HashSet<NetUserId>();
        foreach (var (user, _) in _becameAntag)
        {
            wasAntag.Add(user);
        }

        var reset = new List<Guid>(wasAntag.Count);
        foreach (var user in wasAntag)
        {
            reset.Add(user.UserId);

            if (_missedRounds.TryGetValue(user, out var roles))
                roles.Clear();
        }

        var increments = new List<AntagRollBonusIncrement>();
        foreach (var (user, role) in _eligible)
        {
            if (!played.Contains(user) || wasAntag.Contains(user))
                continue;

            if (_loaded.Contains(user) && _missedRounds.TryGetValue(user, out var roles))
            {
                var missed = roles.GetValueOrDefault(role);
                if (missed >= MaxMissedRounds)
                    continue;

                roles[role] = missed + 1;
            }

            increments.Add(new AntagRollBonusIncrement(user.UserId, role.Id));
        }

        ClearRoundState();
        Flush(reset, increments);
    }

    public void ClearRoundState()
    {
        _preSelected.Clear();
        _becameAntag.Clear();
        _eligible.Clear();
    }

    private void Flush(List<Guid> reset, List<AntagRollBonusIncrement> increments)
    {
        if (reset.Count == 0 && increments.Count == 0)
            return;

        _writeTask = ChainFlush(_writeTask, reset, increments);
    }

    private async Task ChainFlush(Task previous, List<Guid> reset, List<AntagRollBonusIncrement> increments)
    {
        await previous;

        try
        {
            await _db.SaveAntagRollBonusAsync(reset, increments, MaxMissedRounds);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save {increments.Count} roll bonus counter(s): {e}");
        }
    }

    public async Task<Dictionary<ProtoId<AntagPrototype>, int>> GetMissedRounds(NetUserId user)
    {
        if (_loaded.Contains(user) && _missedRounds.TryGetValue(user, out var roles))
            return new Dictionary<ProtoId<AntagPrototype>, int>(roles);

        await _writeTask;

        var stored = await _db.GetAntagRollBonusAsync(user.UserId);

        return stored.ToDictionary(p => new ProtoId<AntagPrototype>(p.Key), p => p.Value);
    }

    public Task<bool> SetMissedRounds(NetUserId user, ProtoId<AntagPrototype> role, int missedRounds)
    {
        missedRounds = Math.Clamp(missedRounds, 0, MaxMissedRounds);

        Apply(user, role, missedRounds);

        var write = ChainSet(_writeTask, user, role, missedRounds);
        _writeTask = write;

        return write;
    }

    private async Task<bool> ChainSet(Task previous, NetUserId user, ProtoId<AntagPrototype> role, int missedRounds)
    {
        await previous;

        try
        {
            await _db.SetAntagRollBonusAsync(user.UserId, role.Id, missedRounds);

            Apply(user, role, missedRounds);
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to set the roll bonus of {user} for {role.Id}: {e}");
            return false;
        }
    }

    private void Apply(NetUserId user, ProtoId<AntagPrototype> role, int missedRounds)
    {
        if (!_missedRounds.TryGetValue(user, out var roles))
            return;

        if (missedRounds == 0)
            roles.Remove(role);
        else
            roles[role] = missedRounds;
    }

    public void CheckWipe()
    {
        if (DateTime.UtcNow < _nextWipe)
            return;

        Wipe();
    }

    private void RescheduleWipe()
    {
        if (_lastWipe is not { } last || _wipeHours <= 0f)
        {
            _nextWipe = DateTime.MaxValue;
            return;
        }

        _nextWipe = last + TimeSpan.FromHours(Math.Min(_wipeHours, MaxWipeHours));
    }

    private async void LoadWipeSchedule()
    {
        try
        {
            var now = DateTime.UtcNow;

            _lastWipe = await _db.GetAntagRollBonusLastWipeAsync(now);
            RescheduleWipe();

            if (_nextWipe == DateTime.MaxValue)
                _sawmill.Info("Roll bonus wiping is disabled.");
            else
                _sawmill.Info($"Next roll bonus wipe at {_nextWipe:u}.");

            CheckWipe();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to read when the roll bonus was last wiped, wiping stays off until restart: {e}");
        }
    }

    public void Wipe()
    {
        _nextWipe = DateTime.MaxValue;

        _generation++;

        foreach (var roles in _missedRounds.Values)
        {
            roles.Clear();
        }

        _writeTask = ChainWipe(_writeTask, DateTime.UtcNow);
    }

    private async Task ChainWipe(Task previous, DateTime now)
    {
        await previous;

        try
        {
            await _db.WipeAntagRollBonusAsync(now);

            _lastWipe = now;
            RescheduleWipe();

            _sawmill.Info($"Wiped every stored roll bonus. Next wipe at {_nextWipe:u}.");
        }
        catch (Exception e)
        {
            _nextWipe = DateTime.UtcNow + WipeRetryInterval;
            _sawmill.Error($"Failed to wipe the stored roll bonuses, retrying at {_nextWipe:u}: {e}");
        }
    }
}
