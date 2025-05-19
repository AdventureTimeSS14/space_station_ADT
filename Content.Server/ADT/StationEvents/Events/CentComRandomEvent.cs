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

        var sightingId = Weight(component.Weights);
        var sightingEvent = $"centcom-random-event-sighting-{sightingId}";

        string sightingText = GetSightingText(sightingId, sightingEvent);

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

    private string GetSightingText(int id, string baseEvent)
    {
        return id switch
        {
            3 => Loc.GetString(baseEvent, ("word", Loc.GetString($"centcom-random-event-word-{RobustRandom.Next(1, 22)}"))),
            2 or 8 => Loc.GetString(baseEvent, ("clothing", Loc.GetString($"centcom-random-event-clothing-{RobustRandom.Next(1, 13)}"))),
            9 => Loc.GetString(baseEvent, ("generator", Loc.GetString($"centcom-random-event-generator-{RobustRandom.Next(1, 7)}"))),
            12 => Loc.GetString(baseEvent, ("plant", Loc.GetString($"centcom-random-event-plant-{RobustRandom.Next(1, 4)}"))),
            14 => Loc.GetString(baseEvent, ("mode", Loc.GetString($"centcom-random-event-mode-{RobustRandom.Next(1, 5)}"))),
            16 => Loc.GetString(baseEvent, ("movement", Loc.GetString($"centcom-random-event-movement-{RobustRandom.Next(1, 4)}"))),
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

        return 1;
    }
}
