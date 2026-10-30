using Content.Shared.ADT.Atmos.Miasma;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed class OrganDecaySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    private static readonly Dictionary<ProtoId<OrganCategoryPrototype>, float> DecayMultipliers = new()
    {
        ["Heart"] = 2.5f,
        ["Lungs"] = 0.9f,
        ["Stomach"] = 1.15f,
    };

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
            if (health.Kind != OrganKind.Organic || health.DecayRate <= 0f || health.Stage == OrganStage.Failing)
                continue;

            if (organ.Body is { } body && _mobQuery.HasComp(body) && !_mobState.IsDead(body))
                continue;

            if (!CanDecay(uid, organ.Body))
                continue;

            var multiplier = organ.Category is { } category && DecayMultipliers.TryGetValue(category, out var value) ? value : 1f;
            _organHealth.ChangeDamage((uid, health), health.MaxDamage * health.DecayRate * multiplier * seconds);
        }
    }

    private bool CanDecay(EntityUid organ, EntityUid? body)
    {
        if (HasComp<EmbalmedComponent>(body ?? organ))
            return false;

        return !_container.TryGetOuterContainer(organ, Transform(organ), out var container)
               || !HasComp<AntiRottingContainerComponent>(container.Owner);
    }
}
