using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
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

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly StationSystem _station = default!;

    protected override void Added(EntityUid uid, CentComRandomEventComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        var sightingId = PickWeighted(component.Weights);
        var sightingEvent = $"centcom-random-event-sighting-{sightingId}";

        string sightingText;

        switch (sightingId)
        {
            case 3:
                sightingText = Loc.GetString(sightingEvent,
                    ("word", Loc.GetString($"centcom-random-event-word-{RobustRandom.Next(1, 22)}")));
                break;

            case 2:
            case 8:
                sightingText = Loc.GetString(sightingEvent,
                    ("clothing", Loc.GetString($"centcom-random-event-clothing-{RobustRandom.Next(1, 13)}")));
                break;

            case 9:
                sightingText = Loc.GetString(sightingEvent,
                    ("generator", Loc.GetString($"centcom-random-event-generator-{RobustRandom.Next(1, 7)}")));
                break;

            case 12:
                sightingText = Loc.GetString(sightingEvent,
                    ("plant", Loc.GetString($"centcom-random-event-plant-{RobustRandom.Next(1, 4)}")));
                break;

            case 14:
                sightingText = Loc.GetString(sightingEvent,
                    ("mode", Loc.GetString($"centcom-random-event-mode-{RobustRandom.Next(1, 5)}")));
                break;

            default:
                sightingText = Loc.GetString(sightingEvent);
                break;
        }

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

        if (_station.GetStations().FirstOrDefault() is { } station)
        {
            SendToFax(station, printout);
        }

        base.Added(uid, component, gameRule, args);
    }

    private bool SendToFax(EntityUid station, FaxPrintout printout)
    {
        var wasSent = false;
        var query = EntityQueryEnumerator<FaxMachineComponent>();

        while (query.MoveNext(out var uid, out var fax))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            _fax.Receive(uid, printout, null, fax);
            wasSent = true;
            break;
        }
        return wasSent;
    }

    private int PickWeighted(Dictionary<int, float> weights)
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

        return 1;
    }
}
