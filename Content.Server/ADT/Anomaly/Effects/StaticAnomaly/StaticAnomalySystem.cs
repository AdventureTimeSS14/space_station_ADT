using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Anomaly.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.ADT.StaticTV.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Random;


namespace Content.Server.ADT.Anomaly.Effects;
public sealed class StaticAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTimer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaticAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);

        SubscribeLocalEvent<NervousSourceComponent, MapInitEvent>(OnMapInit);
    }

    // Система содержит функционал сразу же трёх объектов, я подписал где что
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Anomaly
        var anomalyQuery = EntityQueryEnumerator<StaticAnomalyComponent>();
        while (anomalyQuery.MoveNext(out var uid, out var comp))
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
        var staticTVQuery = EntityQueryEnumerator<StaticTVComponent>();
        while (staticTVQuery.MoveNext(out var uid, out var comp))
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
        var fakeStaticTVQuery = EntityQueryEnumerator<NervousSourceComponent>();
        var curTime = _gameTimer.CurTime;
        while (fakeStaticTVQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.NextEmote > curTime)
                continue;

            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NervousRange))
            {
                if (HasComp<MobStateComponent>(ent) == true)
                {
                    if (_random.Next(1, 10) < 5) // _random.prob почему-то не работал
                        continue;
                    ResetTimer(uid, comp);
                    _chatSystem.TryEmoteWithoutChat(ent, "Scream");
                }
            } 
        }
    }

    //faketv

    // Почти полная копипаста с autoemote
    public bool ResetTimer(EntityUid uid, NervousSourceComponent? comp) 
    {
        if (!Resolve(uid, ref comp))
            return false;

        var curTime = _gameTimer.CurTime;
        var time = curTime + TimeSpan.FromSeconds(10);

        if (comp.NextEmote > time || comp.NextEmote <= curTime)
            comp.NextEmote = time;

        return true;
    }

    private void OnMapInit(EntityUid uid, NervousSourceComponent comp, MapInitEvent args)
    {
        ResetTimer(uid, comp);
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

// 0_o
