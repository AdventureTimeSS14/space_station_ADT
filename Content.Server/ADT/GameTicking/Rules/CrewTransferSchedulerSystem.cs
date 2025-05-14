using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Map;
using Content.Shared.GameTicking.Components;
using Robust.Shared.EntitySerialization.Systems;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Server.Chat.Managers;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Every X minutes starts vote for crew transfer
/// </summary>
public sealed class CrewTransferSchedulerSystem : GameRuleSystem<CrewTransferSchedulerComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, CrewTransferSchedulerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var options = new VoteOptions
        {
            InitiatorText = Loc.GetString("crew-transfer-sender"),
            Title = Loc.GetString("ui-vote-crew-transfer-title"),
            Options =
            {
                (Loc.GetString("ui-vote-crew-transfer-yes"), "yes"),
                (Loc.GetString("ui-vote-crew-transfer-no"), "no")
            },
            Duration = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerRestart)),
            InitiatorTimeout = TimeSpan.FromMinutes(5)
        };
        var vote = _voteManager.CreateVote(options);
        vote.OnFinished += (_, eventArgs) =>
        {
            if (eventArgs.Winner == null)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-crew-transfer-fail"));
                return;
            }
            if ((string)eventArgs.Winner == Loc.GetString("ui-vote-crew-transfer-no"))
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-crew-transfer-no"));
            } else
            {
                _roundEndSystem.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall,
                TimeSpan.FromMinutes(10),
                "crew-transfer-sender",
                "crew-transfer-sended");
            }
        };
    }
}
