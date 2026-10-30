using System.Threading;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class RevolutionaryCentComEventsSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom RobustRandom = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RevolutionaryCentComEventsComponent, GameRuleStartedEvent>(OnRuleStarted);
        SubscribeLocalEvent<RevolutionaryCentComEventsComponent, GameRuleEndedEvent>(OnRuleEnded);
    }

    private void OnRuleStarted(Entity<RevolutionaryCentComEventsComponent> entity, ref GameRuleStartedEvent args)
    {
        ScheduleNext(entity);
    }

    private void OnRuleEnded(Entity<RevolutionaryCentComEventsComponent> entity, ref GameRuleEndedEvent args)
    {
        entity.Comp.Token?.Cancel();
        entity.Comp.Token = null;
    }

    private void ScheduleNext(Entity<RevolutionaryCentComEventsComponent> entity)
    {
        var comp = entity.Comp;

        if (comp.Events.Count == 0 || comp.EventsSent >= comp.MaxEvents)
            return;

        comp.Token ??= new CancellationTokenSource();
        var token = comp.Token.Token;

        Timer.Spawn(NextDelay(comp), () =>
        {
            if (token.IsCancellationRequested || Deleted(entity) || Terminating(entity))
                return;

            _gameTicker.StartGameRule(RobustRandom.Pick(comp.Events));
            comp.EventsSent++;

            ScheduleNext(entity);
        }, token);
    }

    private TimeSpan NextDelay(RevolutionaryCentComEventsComponent component)
    {
        var min = (int) component.MinDelay.TotalSeconds;
        var max = Math.Max(min, (int) component.MaxDelay.TotalSeconds);
        return TimeSpan.FromSeconds(RobustRandom.Next(min, max + 1));
    }
}
