using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Content.Shared.StatusEffect;


namespace Content.Server.ADT.Anomaly.Effects;
public sealed class StaticAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StaticAnomalyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NoiseRange))
            {

                _status.TryAddStatusEffect<SeeingStaticComponent>(ent, "SeeingStatic", TimeSpan.FromSeconds(5f), true);

                if (_entityManager.TryGetComponent<SeeingStaticComponent>(ent, out var staticComp))
                    staticComp.Multiplier = comp.NoiseStrong;
            }
        }
    } 
}
