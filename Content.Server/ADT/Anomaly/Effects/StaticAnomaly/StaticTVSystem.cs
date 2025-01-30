using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.StaticTV.Components;
using Content.Shared.StatusEffect;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;

namespace Content.Server.ADT.StaticTV;
public sealed class StaticAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    public override void Initialize()
    {
        base.Initialize();

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StaticTVComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.Range))
            {

                _status.TryAddStatusEffect<SeeingStaticComponent>(ent, "SeeingStatic", TimeSpan.FromSeconds(5f), true);

                if (_entityManager.TryGetComponent<SeeingStaticComponent>(ent, out var staticComp))
                    staticComp.Multiplier = comp.NoiseStrong;

                _blood.TryModifyBloodLevel(ent, (comp.BloodlossStrong));
                _blood.TryModifyBleedAmount(ent, comp.BleedingStrong);
            }
        }
    }
}
