using Content.Shared.ADT.Crawling; // ADT-Tweak
using Content.Shared.Standing; // ADT-Tweak
using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Content.Shared.Buckle.Components; // ADT-Tweak

namespace Content.Server.Traits.Assorted;

/// <summary>
/// Handles narcolepsy, causing the affected to fall asleep uncontrollably at random intervals.
/// Now respects StrapComponent: sleeping while buckled does not break walking speed.
/// </summary>
public sealed class NarcolepsySystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!; // ADT-Tweak

    public override void Initialize()
    {
        SubscribeLocalEvent<NarcolepsyComponent, ComponentStartup>(SetupNarcolepsy);
    }

    private void SetupNarcolepsy(EntityUid uid, NarcolepsyComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public void AdjustNarcolepsyTimer(EntityUid uid, int TimerReset, NarcolepsyComponent? narcolepsy = null)
    {
        if (!Resolve(uid, ref narcolepsy, false))
            return;

        narcolepsy.NextIncidentTime = TimerReset;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NarcolepsyComponent>();
        while (query.MoveNext(out var uid, out var narcolepsy))
        {
            narcolepsy.NextIncidentTime -= frameTime;

            if (narcolepsy.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            narcolepsy.NextIncidentTime +=
                _random.NextFloat(narcolepsy.TimeBetweenIncidents.X, narcolepsy.TimeBetweenIncidents.Y);

            var duration = _random.NextFloat(narcolepsy.DurationOfIncident.X, narcolepsy.DurationOfIncident.Y);

            // Make sure the sleep time doesn't cut into the time to next incident.
            narcolepsy.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(duration));

            // ADT-Tweak-start
            if (TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count > 0)
            {
                continue;
            }

            _standing.Down(uid, dropHeldItems: false);
            // ADT-Tweak-end
        }
    }
}
