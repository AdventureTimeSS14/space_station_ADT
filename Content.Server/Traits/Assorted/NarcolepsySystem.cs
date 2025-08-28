using Content.Shared.ADT.Crawling; // Ganimed edit
using Content.Shared.Standing; // Ganimed edit
using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Content.Shared.Buckle.Components; // Ganimed edit

namespace Content.Server.Traits.Assorted;

/// <summary>
/// Handles narcolepsy, causing the affected to fall asleep uncontrollably at random intervals.
/// Now respects StrapComponent: sleeping while buckled does not break walking speed.
/// </summary>
public sealed class NarcolepsySystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKey = "ForcedSleep"; // Same one used by N2O and other sleep chems.

    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!; // Ganimed edit

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

            // Ganimed edit start
            _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(duration), false);

            if (TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count > 0)
            {
                continue;
            }

            _standing.Down(uid, dropHeldItems: false);
            // Ganimed edit end
        }
    }
}