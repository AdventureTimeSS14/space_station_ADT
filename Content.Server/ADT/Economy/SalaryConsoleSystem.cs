using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.ADT.Economy;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SQLitePCL;

namespace Content.Server.ADT.Economy;

public sealed class SalaryConsoleSystem : EntitySystem
{
    [Dependency] private readonly BankCardSystem _bankCardSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SalaryConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<SalaryConsoleComponent, AddSalaryEntryMessage>(OnAddSalary);
        SubscribeLocalEvent<SalaryConsoleComponent, RemoveSalaryEntryMessage>(OnRemoveSalary);
        SubscribeLocalEvent<SalaryConsoleComponent, PaySalariesMessage>(OnPaySalaries);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnUiOpened(EntityUid uid, SalaryConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (component.SalaryEntries.Count == 0)
        {
            LoadDefaultSalaries(component);
        }
        UpdateUiState(uid, component);
    }

    private void LoadDefaultSalaries(SalaryConsoleComponent component)
    {
        if (!_prototypeManager.TryIndex<SalaryPrototype>("Salaries", out var salaryProto))
            return;

        foreach (var (jobId, amount) in salaryProto.Salaries)
        {
            var entry = new SalaryEntry
            {
                Type = SalaryType.JobGroup,
                JobGroup = jobId,
                Amount = amount,
                Mode = PaymentMode.Manual,
                Interval = TimeSpan.FromMinutes(15),
                IsActive = true
            };
            component.SalaryEntries.Add(entry);
        }
    }

    private void OnAddSalary(EntityUid uid, SalaryConsoleComponent component, AddSalaryEntryMessage args)
    {
        component.SalaryEntries.Add(args.Entry);
        UpdateUiState(uid, component);
    }

    private void OnRemoveSalary(EntityUid uid, SalaryConsoleComponent component, RemoveSalaryEntryMessage args)
    {
        if (args.Index < component.SalaryEntries.Count)
            component.SalaryEntries.RemoveAt(args.Index);

        UpdateUiState(uid, component);
    }

    private void OnPaySalaries(EntityUid uid, SalaryConsoleComponent component, PaySalariesMessage args)
    {
        PaySalaries(uid, component);
    }

    private void UpdateUiState(EntityUid uid, SalaryConsoleComponent component)
    {
        var station = _stationSystem.GetOwningStation(uid);

        int balance = 0;

        if (station != null && TryComp<StationBankAccountComponent>(station.Value, out var bankAccount))
        {
            balance = bankAccount.Balance;
        }

        var state = new SalaryConsoleUiState
        {
            Balance = balance,
            SalaryEntries = component.SalaryEntries,
            JobList = _bankCardSystem.GetAllAccounts().Select(GetEmployeeInfo).Where(e => e != null).ToList()!
        };
        _uiSystem.SetUiState(uid, SalaryConsoleUiKey.Key, state);
    }

    private void PaySalaries(EntityUid uid, SalaryConsoleComponent component)
    {
        var station = _stationSystem.GetOwningStation(uid);
        if (station == null || !TryComp<StationBankAccountComponent>(station.Value, out var bank))
            return;

        foreach (var entry in component.SalaryEntries)
        {
            if (!entry.IsActive) continue;

            switch (entry.Type)
            {
                case SalaryType.Personal:
                    if (entry.AccountId != null && _bankCardSystem.TryGetAccount(entry.AccountId.Value, out var account))
                    {
                        if (bank.Balance >= entry.Amount)
                        {
                            _cargo.UpdateBankAccount((station.Value, bank), -entry.Amount, bank.PrimaryAccount);
                            account.Balance += entry.Amount;
                        }
                    }
                    break;
                case SalaryType.JobGroup:
                    var accounts = _bankCardSystem.GetAllAccounts().Where(acc => GetEmployeeInfo(acc)?.JobId == entry.JobGroup).ToList();
                    int totalAmount = accounts.Count * entry.Amount;

                    if (bank.Balance >= totalAmount)
                    {
                        _cargo.UpdateBankAccount((station.Value, bank), -totalAmount, bank.PrimaryAccount);
                        foreach (var acc in accounts)
                            acc.Balance += entry.Amount;
                    }
                    break;
                // case SalaryType.Department:
                    // TODO: крч пока в душе не ебу как получать департамент, потом придумаю чето
            }
        }

    }

    private EmployeeInfo? GetEmployeeInfo(BankAccount account)
    {
        if (account.Mind == null)
            return null;

        if (_job.MindTryGetJob(account.Mind, out var job))
        {
            return new EmployeeInfo
            {
                AccountId = account.AccountId,
                Name = account.Name,
                JobId = job.ID,
                JobTitle = job.Name,
                Department = job.ID
            };
        }

        return null;
    }
}
