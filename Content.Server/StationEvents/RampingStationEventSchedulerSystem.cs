using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Robust.Server.Player; // ADT-tweak

namespace Content.Server.StationEvents;

public sealed class RampingStationEventSchedulerSystem : GameRuleSystem<RampingStationEventSchedulerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!; // ADT-tweak

    /// <summary>
    /// Returns the ChaosModifier which increases as round time increases to a point.
    /// </summary>
    public float GetChaosModifier(EntityUid uid, RampingStationEventSchedulerComponent component)
    {
        var roundTime = (float) _gameTicker.RoundDuration().TotalSeconds;

        if (component.EndTime <= 0)
            component.EndTime = 3600f;

        if (component.MaxChaos <= 0)
            component.MaxChaos = 1f;

        if (roundTime > component.EndTime)
            return component.MaxChaos;

        var result = component.MaxChaos / component.EndTime * roundTime + component.StartingChaos;

        if (float.IsNaN(result) || float.IsInfinity(result) || result <= 0)
            return 1f;

        return result;
    }

    protected override void Started(EntityUid uid, RampingStationEventSchedulerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Worlds shittiest probability distribution
        // Got a complaint? Send them to
        component.MaxChaos = _random.NextFloat(component.AverageChaos - component.AverageChaos / 4, component.AverageChaos + component.AverageChaos / 4);
        // This is in minutes, so *60 for seconds (for the chaos calc)
        component.EndTime = _random.NextFloat(component.AverageEndTime - component.AverageEndTime / 4, component.AverageEndTime + component.AverageEndTime / 4) * 60f;

        component.StartingChaos = component.MaxChaos / 10;

        PickNextEventTime(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<RampingStationEventSchedulerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
            // ADT-tweak start
            {
                scheduler.TimeUntilNextEvent = 10f;
                continue;
            }

            var playerCount = _playerManager.PlayerCount;
            if (playerCount < gameRule.MinPlayers)
            {
                scheduler.TimeUntilNextEvent = 10f;
                continue;
            }
            // ADT-tweak end

            if (scheduler.TimeUntilNextEvent > 0f)
            {
                scheduler.TimeUntilNextEvent -= frameTime;
                continue;
            }

            // ADT-tweak start
            var roundTime = _gameTicker.RoundDuration().TotalSeconds;

            if (scheduler.LastEventTime > 0 && roundTime - scheduler.LastEventTime < 10)
            {
                scheduler.EventFloodCounter++;
                if (scheduler.EventFloodCounter > 3)
                {
                    scheduler.TimeUntilNextEvent = 120f;
                    scheduler.EventFloodCounter = 0;
                    continue;
                }
            }
            else
            {
                scheduler.EventFloodCounter = 0;
            }
            // ADT-tweak end

            PickNextEventTime(uid, scheduler);
            scheduler.LastEventTime = roundTime; // ADT-tweak start

            _event.RunRandomEvent(scheduler.ScheduledGameRules);
        }
    }

    /// <summary>
    /// Sets the timing of the next event addition.
    /// </summary>
    private void PickNextEventTime(EntityUid uid, RampingStationEventSchedulerComponent component)
    {
        var mod = GetChaosModifier(uid, component);

        // ADT-tweak start
        if (float.IsNaN(mod) || float.IsInfinity(mod) || mod <= 0)
            mod = 1f;

        var min = 240f / mod;
        var max = 720f / mod;

        if (min <= 0 || max <= 0 || float.IsNaN(min) || float.IsNaN(max))
        {
            component.TimeUntilNextEvent = 300f;
            return;
        }

        component.TimeUntilNextEvent = _random.NextFloat(min, max);

        if (component.TimeUntilNextEvent <= 0 || float.IsNaN(component.TimeUntilNextEvent))
            component.TimeUntilNextEvent = 600f;
    }
        // ADT-tweak end
}