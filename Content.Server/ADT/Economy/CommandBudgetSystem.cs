using Content.Shared.Paper;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;

namespace Content.Server.ADT.Economy;

public sealed class CommandBudgetSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommandBudgetPinPaperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, CommandBudgetPinPaperComponent component, MapInitEvent args)
    {
        if (!TryComp(_station.GetOwningStation(uid), out StationBankAccountComponent? account))
            return;

        var pin = account.BankAccount.AccountPin;
        _paper.SetContent((uid, EnsureComp<PaperComponent>(uid)), Loc.GetString("command-budget-pin-message", ("pin", pin)));
    }
}
