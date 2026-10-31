using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class RevolutionaryCentComEventsSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom RobustRandom = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameRuleStartedEvent>(OnRuleStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnRuleEnded);
    }

    private void OnRuleStarted(ref GameRuleStartedEvent args)
    {
        if (!HasComp<RevolutionaryRuleComponent>(args.RuleEntity))
            return;

        var scheduler = EnsureComp<RevolutionaryCentComEventsComponent>(args.RuleEntity);
        scheduler.NextEvent = _timing.CurTime + scheduler.FirstDelay;
    }

    private void OnRuleEnded(ref GameRuleEndedEvent args)
    {
        if (!HasComp<RevolutionaryRuleComponent>(args.RuleEntity))
            return;

        RemComp<RevolutionaryCentComEventsComponent>(args.RuleEntity);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RevolutionaryCentComEventsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EventsSent >= comp.MaxEvents || _timing.CurTime < comp.NextEvent)
                continue;

            _gameTicker.StartGameRule(comp.EventPrototype);
            comp.EventsSent++;
            comp.NextEvent = _timing.CurTime + RobustRandom.Next(comp.MinDelay, comp.MaxDelay);
        }
    }
}
