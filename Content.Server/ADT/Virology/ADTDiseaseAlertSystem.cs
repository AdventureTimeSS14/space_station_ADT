using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseAlertSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StationSystem _station = default!;

    private static readonly SoundSpecifier OutbreakSound = new SoundPathSpecifier("/Audio/Announcements/outbreak7.ogg");

    private static readonly Color AlertColor = Color.Red;

    public TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public float SecondStageFraction = 0.35f;

    public int SecondStageMinimum = 8;

    private TimeSpan _nextCheck;

    private int _stage;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTDiseasesChangedEvent>(OnDiseasesChanged);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stage = 0;
        _nextCheck = TimeSpan.Zero;
    }

    private void OnDiseasesChanged(Entity<ADTDiseaseCarrierComponent> ent, ref ADTDiseasesChangedEvent args)
    {
        if (_stage >= 1)
            return;

        if (!HasComp<MindContainerComponent>(ent) || _mobState.IsDead(ent))
            return;

        if (!IsContagious(ent, ent.Comp))
            return;

        if (_station.GetOwningStation(ent) is not { } station)
            return;

        _stage = 1;
        Announce(station, "adt-disease-alert-outbreak", 1);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        if (_stage >= 2)
            return;

        var crew = 0;
        var infected = 0;
        EntityUid? station = null;

        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var uid, out var mind))
        {
            if (!mind.HasMind || _mobState.IsDead(uid))
                continue;

            if (_station.GetOwningStation(uid) is not { } owning)
                continue;

            crew++;
            station ??= owning;

            if (TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier) && IsContagious(uid, carrier))
                infected++;
        }

        if (station == null || crew == 0 || infected == 0)
            return;

        if (_stage < 1)
        {
            _stage = 1;
            Announce(station.Value, "adt-disease-alert-outbreak", infected);
            return;
        }

        var fraction = infected / (float) crew;

        if (infected >= SecondStageMinimum && fraction >= SecondStageFraction)
        {
            _stage = 2;
            Announce(station.Value, "adt-disease-alert-confirmed", infected);
        }
    }

    private bool IsContagious(EntityUid uid, ADTDiseaseCarrierComponent carrier)
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

    private void Announce(EntityUid station, string message, int infected)
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
