using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseOutbreakRule : StationEventSystem<ADTDiseaseOutbreakRuleComponent>
{
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StationSystem _station = default!;

    private static readonly SoundSpecifier OutbreakSound = new SoundPathSpecifier("/Audio/Announcements/outbreak7.ogg");

    private static readonly Color AlertColor = Color.Red;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTDiseasesChangedEvent>(OnDiseasesChanged);
    }

    protected override void Started(EntityUid uid, ADTDiseaseOutbreakRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!component.InfectAtStart)
            return;

        var disease = CreateDisease(component);
        if (disease == null)
            return;

        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidProfileComponent>();
        while (query.MoveNext(out var mob, out var mind, out _))
        {
            if (!mind.HasMind || _mobState.IsDead(mob))
                continue;

            candidates.Add(mob);
        }

        RobustRandom.Shuffle(candidates);

        foreach (var candidate in candidates)
        {
            if (_disease.Contract(candidate, disease, isCarrier: true) != null)
                return;
        }
    }

    protected override void ActiveTick(EntityUid uid, ADTDiseaseOutbreakRuleComponent rule, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, rule, gameRule, frameTime);

        if (!rule.Announce || _timing.CurTime < rule.NextCheck)
            return;

        rule.NextCheck = _timing.CurTime + rule.CheckInterval;

        if (rule.Stage >= 2)
            return;

        var crew = 0;
        var infected = 0;
        EntityUid? station = null;

        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var mobUid, out var mind))
        {
            if (!mind.HasMind || _mobState.IsDead(mobUid))
                continue;

            if (_station.GetOwningStation(mobUid) is not { } owning)
                continue;

            crew++;
            station ??= owning;

            if (TryComp<ADTDiseaseCarrierComponent>(mobUid, out var carrier) && IsContagious(carrier))
                infected++;
        }

        if (station == null || crew == 0 || infected == 0)
            return;

        if (rule.Stage < 1)
        {
            rule.Stage = 1;
            Announce(station.Value, rule.OutbreakMessage, infected);
            return;
        }

        var fraction = infected / (float) crew;

        if (infected >= rule.SecondStageMinimum && fraction >= rule.SecondStageFraction)
        {
            rule.Stage = 2;
            Announce(station.Value, rule.ConfirmedMessage, infected);
        }
    }

    private void OnDiseasesChanged(Entity<ADTDiseaseCarrierComponent> ent, ref ADTDiseasesChangedEvent args)
    {
        if (!HasComp<MindContainerComponent>(ent) || _mobState.IsDead(ent))
            return;

        if (!IsContagious(ent.Comp))
            return;

        if (_station.GetOwningStation(ent) is not { } station)
            return;

        var query = EntityQueryEnumerator<ADTDiseaseOutbreakRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleUid, out var rule, out var gameRule))
        {
            if (!rule.Announce || !GameTicker.IsGameRuleActive(ruleUid, gameRule) || rule.Stage >= 1)
                continue;

            rule.Stage = 1;
            Announce(station, rule.OutbreakMessage, 1);
        }
    }

    private bool IsContagious(ADTDiseaseCarrierComponent carrier)
    {
        foreach (var disease in carrier.Diseases)
        {
            if (!_proto.TryIndex(disease.Prototype, out var proto) || !proto.IsVirus)
                continue;

            if ((disease.Spread & (ADTDiseaseSpread.Contact | ADTDiseaseSpread.Airborne | ADTDiseaseSpread.Bites)) != 0)
                return true;
        }

        return false;
    }

    private void Announce(EntityUid station, LocId message, int infected)
    {
        _chat.DispatchStationAnnouncement(
            station,
            Loc.GetString(message, ("count", infected)),
            Loc.GetString("adt-disease-alert-sender"),
            playDefaultSound: false,
            announcementSound: OutbreakSound,
            colorOverride: AlertColor);
    }

    private ADTDisease? CreateDisease(ADTDiseaseOutbreakRuleComponent component)
    {
        if (component.Diseases.Count == 0 || RobustRandom.Prob(component.AdvanceChance))
        {
            return _disease.CreateRandomVirus(
                resistance: RobustRandom.Next(0, 12),
                stealth: RobustRandom.Pick(new[] { 0, 0, 1, 1, 2 }),
                stageSpeed: RobustRandom.Next(-11, 6),
                transmittable: RobustRandom.Next(5, 10),
                severity: RobustRandom.Next(0, 6));
        }

        var id = RobustRandom.Pick(component.Diseases);
        if (!_proto.TryIndex(id, out var proto))
            return null;

        return proto.Symptoms.Count > 0
            ? _disease.CreatePresetAdvance(proto)
            : ADTDisease.FromPrototype(proto);
    }
}
