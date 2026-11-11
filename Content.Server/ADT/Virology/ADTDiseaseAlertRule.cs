using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseAlertRule : GameRuleSystem<ADTDiseaseAlertRuleComponent>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StationSystem _station = default!;

    private static readonly SoundSpecifier OutbreakSound = new SoundPathSpecifier("/Audio/Announcements/outbreak7.ogg");

    private static readonly Color AlertColor = Color.Red;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTDiseasesChangedEvent>(OnDiseasesChanged);
    }

    private void OnDiseasesChanged(Entity<ADTDiseaseCarrierComponent> ent, ref ADTDiseasesChangedEvent args)
    {
        if (!HasComp<MindContainerComponent>(ent) || _mobState.IsDead(ent))
            return;

        if (!IsContagious(ent.Comp))
            return;

        if (_station.GetOwningStation(ent) is not { } station)
            return;

        var query = EntityQueryEnumerator<ADTDiseaseAlertRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleUid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(ruleUid, gameRule) || rule.Stage >= 1)
                continue;

            rule.Stage = 1;
            Announce(station, rule.OutbreakMessage, 1);
        }
    }

    protected override void ActiveTick(EntityUid uid, ADTDiseaseAlertRuleComponent rule, GameRuleComponent gameRule, float frameTime)
    {
        if (_timing.CurTime < rule.NextCheck)
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
}
