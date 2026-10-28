using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Events;
using Content.Server.ADT.StationEvents.Components;
using Content.Server.Chat.Systems;
using System.Linq;
using Content.Server.Fax;
using Content.Shared.Fax.Components;

namespace Content.Server.ADT.StationEvents.Events;

public sealed class CentComRandomEvent : StationEventSystem<CentComRandomEventComponent>
{
    const int TIME_YEAR_SPACE_STATION_ADT = 544;

    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private FaxSystem _fax = default!;
    [Dependency] private StationSystem _station = default!;

    private readonly HashSet<(int Id, int Variant)> _usedBans = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _usedBans.Clear();
    }

    protected override void Added(EntityUid uid, CentComRandomEventComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        if (_usedBans.Count >= component.MaxBansPerRound || !TryPickBan(component, out var sightingId, out var variant))
        {
            base.Added(uid, component, gameRule, args);
            return;
        }

        _usedBans.Add((sightingId, variant));

        string sightingText = GetSightingText(sightingId, variant);

        var announcement = Loc.GetString("centcom-random-event-announcement",
            ("sighting", sightingText));

        _chatSystem.DispatchGlobalAnnouncement(
            message: announcement,
            sender: Loc.GetString("centcom-random-event-sender"),
            colorOverride: Color.Crimson
        );

        string text = Loc.GetString("centcom-random-event-doc",
            ("sighting", sightingText));

        DateTime time = DateTime.UtcNow.AddYears(TIME_YEAR_SPACE_STATION_ADT);
        text = text.Replace("$time$", $"{(time.Day < 10 ? $"0{time.Day}" : time.Day)}.{(time.Month < 10 ? $"0{time.Month}" : time.Month)}.{time.Year}");

        var printout = new FaxPrintout(
            text,
            Loc.GetString("centcom-random-event-fax-paper-name"),
            null,
            null,
            "paper_stamp-centcom",
            [new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") }]
        );

        SendToFax(printout);

        base.Added(uid, component, gameRule, args);
    }

    private void SendToFax(FaxPrintout printout)
    {
        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var faxUid, out var fax))
        {
            if (!fax.ReceiveStationGoal)
                continue;

            _fax.Receive(faxUid, printout, null, fax);
        }
    }

    private bool TryPickBan(CentComRandomEventComponent component, out int id, out int variant)
    {
        id = 0;
        variant = 0;

        var available = new Dictionary<int, float>();
        foreach (var (sightingId, weight) in component.Weights)
        {
            if (weight <= 0f)
                continue;

            if (GetFreeVariants(component, sightingId).Count > 0)
                available[sightingId] = weight;
        }

        if (available.Count == 0)
            return false;

        id = Weight(available);
        variant = RobustRandom.Pick(GetFreeVariants(component, id));
        return true;
    }

    private List<int> GetFreeVariants(CentComRandomEventComponent component, int id)
    {
        var result = new List<int>();
        var count = component.VariantCounts.GetValueOrDefault(id, 0);

        if (count <= 0)
        {
            if (!_usedBans.Contains((id, 0)))
                result.Add(0);

            return result;
        }

        for (var i = 1; i <= count; i++)
        {
            if (!_usedBans.Contains((id, i)))
                result.Add(i);
        }

        return result;
    }

    private string GetSightingText(int id, int variant)
    {
        var baseEvent = $"centcom-random-event-sighting-{id}";

        return id switch
        {
            3 => Loc.GetString(baseEvent, ("generator", Loc.GetString($"centcom-random-event-generator-{variant}"))),
            4 => Loc.GetString(baseEvent, ("plant", Loc.GetString($"centcom-random-event-plant-{variant}"))),
            6 => Loc.GetString(baseEvent, ("language", Loc.GetString($"centcom-random-event-language-{variant}"))),
            7 => Loc.GetString(baseEvent, ("words", Loc.GetString($"centcom-random-event-words-{variant}"))),
            8 => Loc.GetString(baseEvent, ("race", Loc.GetString($"centcom-random-event-race-{variant}"))),
            101 => Loc.GetString(baseEvent, ("medicine", Loc.GetString($"centcom-random-event-medicine-{variant}"))),
            106 => Loc.GetString(baseEvent, ("gas", Loc.GetString($"centcom-random-event-gas-{variant}"))),
            _ => Loc.GetString(baseEvent),
        };
    }

    private int Weight(Dictionary<int, float> weights)
    {
        var totalWeight = weights.Values.Sum();
        var randomValue = RobustRandom.NextFloat() * totalWeight;
        var currentWeight = 0f;

        foreach (var (id, weight) in weights)
        {
            currentWeight += weight;
            if (randomValue <= currentWeight)
                return id;
        }

        return weights.Keys.Last();
    }
}
