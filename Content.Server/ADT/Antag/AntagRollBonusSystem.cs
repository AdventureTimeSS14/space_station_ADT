using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.ADT.Antag;
using Content.Shared.ADT.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Antag;

public sealed class AntagRollBonusSystem : EntitySystem
{
    [Dependency] private readonly AntagRollBonusManager _rollBonus = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;

    public static readonly TimeSpan AbortedRoundCountsAfter = TimeSpan.FromHours(1);

    private const string RollBonusInfoRateLimitKey = "AntagRollBonusInfo";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeNetworkEvent<RequestAntagRollBonusInfoEvent>(OnRequestRollBonusInfo);

        _rateLimit.Register(
            RollBonusInfoRateLimitKey,
            new RateLimitRegistration(
                ADTCCVars.AntagRollBonusInfoRateLimitPeriod,
                ADTCCVars.AntagRollBonusInfoRateLimitCount,
                null));
    }

    private void OnRequestRollBonusInfo(RequestAntagRollBonusInfoEvent ev, EntitySessionEventArgs args)
    {
        if (_rateLimit.CountAction(args.SenderSession, RollBonusInfoRateLimitKey) != RateLimitStatus.Allowed)
            return;

        var roles = _proto.EnumeratePrototypes<AntagPrototype>()
            .Where(a => a.SetPreference)
            .Select(a => new ProtoId<AntagPrototype>(a.ID));

        var weights = _rollBonus.GetWeights(args.SenderSession, roles);

        RaiseNetworkEvent(new AntagRollBonusInfoEvent(weights), args.SenderSession);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                FinishRound();
                break;

            case GameRunLevel.PreRoundLobby when ev.Old == GameRunLevel.InRound:
                if (_gameTicker.RoundDuration() < AbortedRoundCountsAfter)
                    break;

                FinishRound();
                break;
        }
    }

    private void FinishRound()
    {
        var participants = _gameTicker.PlayerGameStatuses
            .Where(x => x.Value == PlayerGameStatus.JoinedGame)
            .Select(x => x.Key)
            .ToList();

        _rollBonus.FinishRound(participants);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _rollBonus.ClearRoundState();
    }
}
