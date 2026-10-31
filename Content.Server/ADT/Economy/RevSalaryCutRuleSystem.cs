using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.ADT.Economy;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Economy;

[UsedImplicitly]
public sealed partial class RevSalaryCutRuleSystem : StationEventSystem<RevSalaryCutRuleComponent>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, RevSalaryCutRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryGetRandomStation(out var station) ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        var departments = bank.Accounts.Keys
            .Where(proto => proto != bank.PrimaryAccount)
            .ToList();

        if (departments.Count == 0)
            return;

        var department = _random.Pick(departments);
        var percent = _random.Next(component.MinPercent, component.MaxPercent + 1);
        var cut = percent / 100f;

        base.Started(uid, component, gameRule, args);

        var cuts = EnsureComp<StationSalaryCutComponent>(station.Value);
        cuts.Cuts[department] = Math.Min(cuts.Cuts.GetValueOrDefault(department) + cut, cuts.MaxCut);

        var departmentName = Loc.GetString($"command-budget-{department}");
        var totalPercent = (int) Math.Round(cuts.Cuts[department] * 100);
        var reason = Loc.GetString($"rev-salary-cut-reason-{_random.Next(1, 4)}");

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("rev-salary-cut-start",
                ("reason", reason),
                ("department", departmentName),
                ("percent", percent),
                ("total", totalPercent)),
            sender: Loc.GetString("rev-economy-sender"),
            colorOverride: Color.FromHex("#c8323c"));
    }
}
