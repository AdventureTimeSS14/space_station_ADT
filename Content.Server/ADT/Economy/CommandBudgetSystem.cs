using Content.Shared.Paper;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Prototypes;

namespace Content.Server.ADT.Economy;

public sealed class CommandBudgetSystem : EntitySystem
{
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private BankCardSystem _bankCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommandBudgetPinPaperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, CommandBudgetPinPaperComponent component, MapInitEvent args)
    {
        if (_station.GetOwningStation(uid) is not { } station || component.CommandBudgetType == null)
            return;

        if (!_bankCard.TryGetDepartmentAccountId(station, component.CommandBudgetType.Value, out var accountId) ||
            !_bankCard.TryGetAccount(accountId, out var account))
            return;

        _paper.SetContent((uid, EnsureComp<PaperComponent>(uid)),
            Loc.GetString("command-budget-pin-message", ("pin", account.AccountPin)));
    }
}
