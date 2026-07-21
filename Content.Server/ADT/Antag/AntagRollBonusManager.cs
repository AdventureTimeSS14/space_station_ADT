using Content.Shared.ADT.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Antag;

public sealed class AntagRollBonusManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    public const int MaxMissedRounds = 20;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private float _bonusPerRound;

    private readonly Dictionary<(NetUserId User, ProtoId<AntagPrototype> Role), int> _missedRounds = new();

    private readonly HashSet<(NetUserId User, ProtoId<AntagPrototype> Role)> _preSelected = new();

    private readonly HashSet<(NetUserId User, ProtoId<AntagPrototype> Role)> _becameAntag = new();

    private readonly HashSet<(NetUserId User, ProtoId<AntagPrototype> Role)> _eligible = new();

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("antag.rollbonus");

        _cfg.OnValueChanged(ADTCCVars.AntagRollBonusEnabled, OnEnabledChanged, true);
        _cfg.OnValueChanged(ADTCCVars.AntagRollBonusPerRound, OnBonusPerRoundChanged, true);

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

    public float GetWeight(ICommonSession session, ProtoId<AntagPrototype> role)
    {
        if (!_enabled)
            return 1f;

        if (!_missedRounds.TryGetValue((session.UserId, role), out var missed))
            return 1f;

        return 1f + _bonusPerRound * missed;
    }

    public int GetMissedRounds(ICommonSession session, ProtoId<AntagPrototype> role)
    {
        return _missedRounds.GetValueOrDefault((session.UserId, role));
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

        var cleared = 0;
        if (wasAntag.Count > 0)
        {
            var stale = new List<(NetUserId User, ProtoId<AntagPrototype> Role)>();
            foreach (var key in _missedRounds.Keys)
            {
                if (wasAntag.Contains(key.User))
                    stale.Add(key);
            }

            foreach (var key in stale)
            {
                _missedRounds.Remove(key);
            }

            cleared = stale.Count;
        }

        var increased = 0;
        var capped = 0;

        foreach (var key in _eligible)
        {
            if (!played.Contains(key.User))
                continue;

            if (wasAntag.Contains(key.User))
                continue;

            var missed = _missedRounds.GetValueOrDefault(key);
            if (missed >= MaxMissedRounds)
            {
                capped++;
                continue;
            }

            _missedRounds[key] = missed + 1;
            increased++;
        }

        ClearRoundState();
    }

    public void ClearRoundState()
    {
        _preSelected.Clear();
        _becameAntag.Clear();
        _eligible.Clear();
    }
}
