using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Anomaly.Components;
using Content.Shared.Mobs.Components;
using Content.Server.Chat;
using Content.Shared.ADT.StaticTV.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;

namespace Content.Server.ADT.Anomaly.Effects;
public sealed class StaticAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaticAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Anomaly
        var AnomalyQuery = EntityQueryEnumerator<StaticAnomalyComponent>();
        while (AnomalyQuery.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NoiseRange))
            {
                if (HasComp<MobStateComponent>(ent) == true)
                {

                    _status.TryAddStatusEffect<SeeingStaticComponent>(ent, "SeeingStatic", TimeSpan.FromSeconds(5f), true);

                    if (_entityManager.TryGetComponent<SeeingStaticComponent>(ent, out var staticComp))
                        staticComp.Multiplier = comp.NoiseStrong;
                }
            }
        }

        //StaticTV
        var StaticTVQuery = EntityQueryEnumerator<StaticTVComponent>();
        while (StaticTVQuery.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.Range))
            {
                if (HasComp<MobStateComponent>(ent) == true)
                {
                    _status.TryAddStatusEffect<SeeingStaticComponent>(ent, "SeeingStatic", TimeSpan.FromSeconds(5f), true);

                    if (_entityManager.TryGetComponent<SeeingStaticComponent>(ent, out var staticComp))
                        staticComp.Multiplier = comp.NoiseStrong;

                    _blood.TryModifyBloodLevel(ent, (comp.BloodlossStrong));
                    _blood.TryModifyBleedAmount(ent, comp.BleedingStrong);
                }
            }
        }

        //FakeStaticTV
        var FakeStaticTVQuery = EntityQueryEnumerator<NervousSourceComponent>();
        while (FakeStaticTVQuery.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NervousRange))
            {
                if (HasComp<MobStateComponent>(ent) == true)
                {
                    EnsureComp<AutoEmoteComponent>(ent);
                    _autoEmote.AddEmote(ent, "NervousSream");
                    if (Transform(uid).Coordinates.TryDistance(EntityManager, Transform(ent).Coordinates, out var distance)
                        && distance >= comp.NervousRange)
                    {
                        _autoEmote.RemoveEmote(ent, "NervousSream");
                    }
                }
            }
        }
    }

    //Anomaly
    private void OnSupercritical(EntityUid uid, StaticAnomalyComponent comp, ref AnomalySupercriticalEvent args)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NoiseRange))
        {
            _status.TryRemoveStatusEffect(ent, "SeeingStatic");
        }
    }
}
