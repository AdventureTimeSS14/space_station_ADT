using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class RevolutionaryCentComEventsSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RevolutionaryRuleComponent, GameRuleStartedEvent>(OnRevolutionStarted);
        SubscribeLocalEvent<RevolutionaryRuleComponent, GameRuleEndedEvent>(OnRevolutionEnded);
    }

    private void OnRevolutionStarted(Entity<RevolutionaryRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        var scheduler = EnsureComp<RevolutionaryCentComEventsComponent>(ent);
        scheduler.NextEvent = _timing.CurTime + scheduler.FirstDelay;
    }

    private void OnRevolutionEnded(Entity<RevolutionaryRuleComponent> ent, ref GameRuleEndedEvent args)
    {
        RemComp<RevolutionaryCentComEventsComponent>(ent);
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
