using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Economy;

[UsedImplicitly]
public sealed partial class CargoCategorySurgeRuleSystem : StationEventSystem<CargoCategorySurgeRuleComponent>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, CargoCategorySurgeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryGetRandomStation(out var station))
            return;

        var categories = CollectCategories();
        if (categories.Count == 0)
            return;

        var count = Math.Min(component.MaxCategories, _random.Next(1, categories.Count + 1));
        var picked = new List<string>(count);
        while (picked.Count < count)
        {
            var category = _random.Pick(categories);
            if (!picked.Contains(category))
                picked.Add(category);
        }

        var expensive = _random.Prob(0.5f);
        var percent = expensive
            ? _random.Next(component.ExpensiveMinPercent, component.ExpensiveMaxPercent + 1)
            : _random.Next(component.CheapMinPercent, component.CheapMaxPercent + 1);
        var multiplier = expensive ? 1 + percent / 100f : 1 - percent / 100f;

        base.Started(uid, component, gameRule, args);

        TimeSpan duration = TimeSpan.Zero;
        if (TryComp<StationEventComponent>(uid, out var stationEvent) && stationEvent.EndTime > Timing.CurTime)
            duration = stationEvent.EndTime - Timing.CurTime;

        var surge = EnsureComp<StationCategorySurgeComponent>(station.Value);
        surge.Multipliers.Clear();
        foreach (var category in picked)
            surge.Multipliers[category] = multiplier;

        component.Station = station;
        component.Categories.AddRange(picked);
        component.Percent = percent;
        component.Expensive = expensive;

        var names = string.Join(", ", picked.Select(Loc.GetString));
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString(expensive ? "cargo-surge-announced-expensive" : "cargo-surge-announced-cheap",
                ("categories", names),
                ("percent", percent),
                ("minutes", (int) duration.TotalMinutes)),
            sender: Loc.GetString("cargo-surge-sender"),
            colorOverride: Color.FromHex(expensive ? "#c8323c" : "#57c84d"));

        AdtRefreshConsoles(station.Value);
    }

    protected override void Ended(EntityUid uid, CargoCategorySurgeRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.Station == null || component.Categories.Count == 0)
            return;

        var names = string.Join(", ", component.Categories.Select(Loc.GetString));

        if (TryComp<StationCategorySurgeComponent>(component.Station, out var surge))
            RemComp<StationCategorySurgeComponent>(component.Station.Value);

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("cargo-surge-ended", ("categories", names)),
            sender: Loc.GetString("cargo-surge-sender"),
            colorOverride: Color.FromHex("#18abf5"));

        AdtRefreshConsoles(component.Station.Value);
    }

    private void AdtRefreshConsoles(EntityUid station)
    {
        _cargo.AdtRefreshOrderConsoles(station);
    }

    private List<string> CollectCategories()
    {
        var set = new HashSet<string>();

        foreach (var proto in _protoMan.EnumeratePrototypes<CargoProductPrototype>())
        {
            if (!string.IsNullOrEmpty(proto.Category))
                set.Add(proto.Category);
        }

        return set.ToList();
    }
}
