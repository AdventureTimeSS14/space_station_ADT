using Content.Shared.ADT.Atmos.Miasma;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed class OrganDecaySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;
        var seconds = (float) UpdateInterval.TotalSeconds;

        var query = EntityQueryEnumerator<OrganHealthComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var health, out var organ))
        {
            if (organ.Body != null)
                continue;

            if (health.Kind != OrganKind.Organic || health.DecayRate <= 0f || health.Stage == OrganStage.Failing)
                continue;

            if (!CanDecay(uid))
                continue;

            _organHealth.ChangeDamage((uid, health), health.MaxDamage * health.DecayRate * seconds);
        }
    }

    private bool CanDecay(EntityUid organ)
    {
        if (HasComp<EmbalmedComponent>(organ))
            return false;

        return !_container.TryGetOuterContainer(organ, Transform(organ), out var container)
               || !HasComp<AntiRottingContainerComponent>(container.Owner);
    }
}
