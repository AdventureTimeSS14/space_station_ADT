using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared.ADT.Economy;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;
using Content.Shared.Access.Systems;
using Content.Server.ADT.Traits;
using Content.Shared.Database;

namespace Content.Server.ADT.Economy;

public sealed class BankCardSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IdCardSystem _idCardSystem = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private BankCartridgeSystem _bankCartridge = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;

    private const int SalaryDelay = 2700;
    private const int HistoryLimit = 100;

    private SalaryPrototype _salaries = default!;
    private readonly List<BankAccount> _accounts = new();
    private float _salaryTimer;

    private int _totalDeposited;
    private int _totalBurned;
    private int _totalTransfers;
    private int _totalSalary;
    private int _totalDeposits;
    private int _totalTaxes;

    // ADT-Economy: доступ для RoundEndStatsSystem (вкладка «Отчёт» итогов раунда)
    public int TotalSalary => _totalSalary;
    public int TotalDeposits => _totalDeposits;
    public int TotalTaxes => _totalTaxes;
    public int TotalTransfers => _totalTransfers;
    public int TotalDeposited => _totalDeposited;
    public int TotalBurned => _totalBurned;

    public override void Initialize()
    {
        _salaries = _protoMan.Index<SalaryPrototype>("Salaries");

        SubscribeLocalEvent<BankCardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned, after: [typeof(ADTTraitSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            _salaryTimer = 0f;
            return;
        }

        _salaryTimer += frameTime;

        if (_salaryTimer <= SalaryDelay)
            return;

        _salaryTimer = 0f;
        PaySalary();
    }

    private void PaySalary()
    {
        if (!_configManager.GetCVar(ADTCCVars.PaySalary))
            return;

        var taxRate = Math.Clamp(_configManager.GetCVar(ADTCCVars.SalaryTaxRate), 0f, 1f);

        DepositDepartmentFunds();

        var groups = new Dictionary<int, FundingGroup>();
        foreach (var account in _accounts.Where(account =>
                     !account.CommandBudgetAccount &&
                     account.Mind is { Comp.UserId: not null, Comp.CurrentEntity: not null } &&
                     _playerManager.TryGetSessionById(account.Mind.Value.Comp.UserId!.Value, out _) &&
                     !_mobState.IsDead(account.Mind.Value.Comp.CurrentEntity!.Value)))
        {
            if (!_job.MindTryGetJob(account.Mind, out var job))
                continue;

            var (isFixed, pay) = GetPay(account.Mind);

            if (isFixed)
            {
                if (pay > 0 && TryDeposit(account.AccountId, (int) pay, TransactionReason.Salary, Loc.GetString("bank-initiator-centcom")))
                    _totalSalary += (int) pay;
                continue;
            }

            if (pay <= 0 ||
                account.Mind!.Value.Comp.CurrentEntity is not { } mob ||
                !TryResolveFunding(mob, job.ID, out var station, out var fundingProto, out var fundingId))
                continue;

            if (!groups.TryGetValue(fundingId, out var group))
            {
                group = new FundingGroup(station, fundingProto);
                groups[fundingId] = group;
            }

            group.Members.Add((account.AccountId, pay, mob, job.ID));
        }

        foreach (var (fundingId, group) in groups)
        {
            if (group.PrimaryAccountId == null &&
                TryComp<StationBankAccountComponent>(group.Station, out var stationBank) &&
                TryGetDepartmentAccountId(group.Station, stationBank.PrimaryAccount, out var primaryId))
                group.PrimaryAccountId = primaryId;

            var totalShare = group.Members.Sum(m => m.Share);
            var factor = totalShare > 1 ? 1 / totalShare : 1;

            var cut = 0f;
            if (TryComp<StationSalaryCutComponent>(group.Station, out var salaryCuts) &&
                salaryCuts.Cuts.TryGetValue(group.Funding, out var fundingCut))
                cut = Math.Clamp(fundingCut, 0f, 0.9f);

            var balance = GetBalance(fundingId);
            var remaining = balance;

            foreach (var (playerAccountId, share, mob, jobId) in group.Members)
            {
                var bonus = GetSalaryBonus(group.Station, jobId);
                var gross = (int) Math.Round(balance * share * factor * (1 + bonus));
                if (gross <= 0)
                    continue;

                var affordable = Math.Min(gross, remaining);
                if (affordable <= 0)
                    break;

                if (cut > 0)
                {
                    var paid = (int) Math.Floor(affordable * (1 - cut));
                    var withheld = affordable - paid;

                    if (withheld > 0)
                        TryBurn(fundingId, withheld, TransactionReason.RevConfiscation, Loc.GetString("bank-initiator-centcom"));

                    if (paid > 0 && TryTransfer(fundingId, playerAccountId, paid, TransactionReason.Salary))
                        _totalSalary += paid;

                    remaining -= affordable;
                    continue;
                }

                var taxPart = (taxRate > 0 && fundingId != group.PrimaryAccountId)
                    ? (int) Math.Floor(affordable * taxRate)
                    : 0;
                var payout = affordable - taxPart;

                if (taxPart > 0 && group.PrimaryAccountId != null &&
                    TryTransfer(fundingId, group.PrimaryAccountId.Value, taxPart, TransactionReason.SalaryTax))
                    _totalTaxes += taxPart;

                if (payout > 0 && TryTransfer(fundingId, playerAccountId, payout, TransactionReason.Salary))
                    _totalSalary += payout;

                remaining -= affordable;
            }
        }

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("salary-pay-announcement"),
            colorOverride: Color.FromHex("#18abf5"));
    }

    private void DepositDepartmentFunds()
    {
        var deposit = _configManager.GetCVar(ADTCCVars.DepartmentDeposit);
        if (deposit <= 0)
            return;

        var total = 0;
        var query = EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var station, out var bank))
        {
            foreach (var proto in bank.Accounts.Keys)
            {
                if (proto == bank.PrimaryAccount)
                    continue;

                if (_cargo.TryAdjustBankAccount((station, bank), proto, deposit, true))
                    total += deposit;
            }
        }

        if (total > 0)
        {
            _totalDeposits += total;
        }
    }

    private sealed class FundingGroup(EntityUid station, ProtoId<CargoAccountPrototype> funding)
    {
        public readonly EntityUid Station = station;
        public readonly ProtoId<CargoAccountPrototype> Funding = funding;

        public int? PrimaryAccountId;

        public readonly List<(int PlayerAccount, double Share, EntityUid Mob, string JobId)> Members = new();
    }

    private bool TryResolveFunding(EntityUid mob, string jobId, out EntityUid station, out ProtoId<CargoAccountPrototype> fundingProto, out int fundingId)
    {
        station = default;
        fundingProto = default!;
        fundingId = 0;

        if (_station.GetOwningStation(mob) is not { } stationUid ||
            !TryComp<StationBankAccountComponent>(stationUid, out var stationBank))
            return false;

        var proto = GetDepartmentForJob(jobId);
        fundingProto = proto ?? stationBank.PrimaryAccount;

        return TryGetDepartmentAccountId(stationUid, fundingProto, out fundingId);
    }

    private ProtoId<CargoAccountPrototype>? GetDepartmentForJob(string jobId)
    {
        foreach (var department in _protoMan.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.Roles.Contains(jobId) && _protoMan.HasIndex<CargoAccountPrototype>(department.ID))
                return department.ID;
        }

        return null;
    }

    // надбавка к зарплате по отделу профессии (StationBankAccountComponent.SalaryBonus)
    private double GetSalaryBonus(EntityUid station, string jobId)
    {
        if (!TryComp<StationBankAccountComponent>(station, out var bank) ||
            bank.SalaryBonus.Count == 0)
            return 0;

        var department = GetDepartmentForJob(jobId);
        return department == null ? 0 : bank.SalaryBonus.GetValueOrDefault(department.Value);
    }

    public bool TryGetUserAccountId(EntityUid user, out int accountId)
    {
        accountId = 0;

        foreach (var item in _accessReader.FindPotentialAccessItems(user))
        {
            if (TryComp(item, out PdaComponent? pda) &&
                pda.ContainedId is { Valid: true } id &&
                TryComp<BankCardComponent>(id, out var idCard) &&
                idCard.AccountId.HasValue)
            {
                accountId = idCard.AccountId.Value;
                return true;
            }

            if (TryComp<BankCardComponent>(item, out var card) && card.AccountId.HasValue)
            {
                accountId = card.AccountId.Value;
                return true;
            }
        }

        return false;
    }

    private (bool Fixed, double Value) GetPay(EntityUid? mind)
    {
        if (!_job.MindTryGetJob(mind, out var job))
            return (false, 0);

        if (_salaries.Fixed.TryGetValue(job.ID, out var fixedPay))
            return (true, fixedPay);

        if (_salaries.Salaries.TryGetValue(job.ID, out var share))
            return (false, share);

        return (false, 0);
    }

    private void OnMapInit(EntityUid uid, BankCardComponent component, MapInitEvent args)
    {
        if (component.CommandBudgetCard)
        {
            if (TryComp<StationBankAccountComponent>(_station.GetOwningStation(uid), out var stationBank)
                && component.CommandBudgetType != null)
            {

                var existingAccount = _accounts.FirstOrDefault(acc =>
                    acc.CommandBudgetAccount &&
                    acc.AccountPrototype == component.CommandBudgetType);

                if (existingAccount != null)
                {
                    component.AccountId = existingAccount.AccountId;
                    return;
                }

                var account = CreateDepartmentAccount(component.CommandBudgetType.Value);
                stationBank.BankAccounts[component.CommandBudgetType.Value] = account;
                component.AccountId = account.AccountId;
                return;
            }
        }

        if (component.AccountId.HasValue)
        {
            CreateAccount(component.AccountId.Value, component.StartingBalance);
            return;
        }

        var playerAccount = CreateAccount(default, component.StartingBalance);
        component.AccountId = playerAccount.AccountId;
    }

    public bool TryGetDepartmentAccountId(EntityUid station, ProtoId<CargoAccountPrototype> proto, out int accountId)
    {
        accountId = 0;

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return false;

        if (!bank.BankAccounts.TryGetValue(proto, out var account))
        {
            account = CreateDepartmentAccount(proto);
            bank.BankAccounts[proto] = account;

            _cargo.TryAdjustBankAccount((station, bank), proto, 0, true);
        }

        accountId = account.AccountId;
        return true;
    }

    private BankAccount CreateDepartmentAccount(ProtoId<CargoAccountPrototype> departmentType)
    {
        int accountNumber;
        do
        {
            accountNumber = _random.Next(100000, 999999);
        } while (AccountExist(accountNumber));

        var account = new BankAccount(accountNumber, 0, _random);
        account.AccountPrototype = departmentType;
        account.CommandBudgetAccount = true;
        account.Name = Loc.GetString($"command-budget-{departmentType}");

        _accounts.Add(account);
        return account;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _accounts.Clear();
        _totalDeposited = 0;
        _totalBurned = 0;
        _totalTransfers = 0;
        _totalSalary = 0;
        _totalDeposits = 0;
        _totalTaxes = 0;
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_idCardSystem.TryFindIdCard(ev.Mob, out var id) && TryComp<MindContainerComponent>(ev.Mob, out var mind))
        {
            var cardEntity = id.Owner;
            var bankCardComponent = EnsureComp<BankCardComponent>(cardEntity);

            if (!bankCardComponent.AccountId.HasValue || !TryGetAccount(bankCardComponent.AccountId.Value, out var bankAccount))
                return;

            if (!TryComp(mind.Mind, out MindComponent? mindComponent))
                return;

            var startingBalance = 100;
            var (isFixed, pay) = GetPay(mind.Mind);

            if (isFixed)
                startingBalance += (int) pay;
            else if (pay > 0 &&
                     _job.MindTryGetJob(mind.Mind, out var job) &&
                     TryResolveFunding(ev.Mob, job.ID, out _, out _, out var fundingId))
                startingBalance += (int) Math.Round(GetBalance(fundingId) * pay);

            if (TryComp<StartingBalanceComponent>(ev.Mob, out var balanceComp))
                startingBalance = balanceComp.Balance;

            bankAccount.Balance = startingBalance;
            mindComponent.AddMemory(new Memory("PIN", bankAccount.AccountPin.ToString()));
            mindComponent.AddMemory(new Memory(Loc.GetString("character-info-memories-account-number"),
                bankAccount.AccountId.ToString()));
            bankAccount.Mind = (mind.Mind.Value, mindComponent);
            bankAccount.Name = Name(ev.Mob);

            if (!_inventorySystem.TryGetSlotEntity(ev.Mob, "id", out var pdaUid))
                return;

            BankCartridgeComponent? comp = null;

            var programs = _cartridgeLoader.GetDiskPrograms(pdaUid.Value);

            var program = programs.ToList().Find(program => TryComp(program, out comp));
            if (comp == null)
                return;

            bankAccount.CartridgeUid = program;
            comp.AccountId = bankAccount.AccountId;
        }
    }

    public bool TryDeposit(int accountId, int amount, TransactionReason reason = TransactionReason.Other, string? initiator = null)
    {
        if (amount <= 0)
            return false;

        return Mutate(accountId, amount, reason, initiator, isTransfer: false);
    }

    public bool TryBurn(int accountId, int amount, TransactionReason reason = TransactionReason.Other, string? initiator = null)
    {
        if (amount <= 0)
            return false;

        return Mutate(accountId, -amount, reason, initiator, isTransfer: false);
    }

    public bool TryTransfer(int fromAccountId, int toAccountId, int amount, TransactionReason reason = TransactionReason.Other, string? initiator = null)
    {
        if (amount <= 0 || fromAccountId == toAccountId)
            return false;

        if (!TryGetAccount(fromAccountId, out var from) || !TryGetAccount(toAccountId, out var to))
            return false;

        if (GetBalance(fromAccountId) < amount)
            return false;

        if (!Mutate(fromAccountId, -amount, reason, initiator, isTransfer: true, to))
            return false;

        if (!Mutate(toAccountId, amount, reason, initiator, isTransfer: true, from))
        {
            // получатель недоступен — возвращаем деньги отправителю
            Mutate(fromAccountId, amount, reason, initiator, isTransfer: false);
            return false;
        }

        _totalTransfers += amount;
        return true;
    }

    private bool Mutate(int accountId, int amount, TransactionReason reason, string? initiator, bool isTransfer, BankAccount? counterpart = null)
    {
        if (amount == 0 || !TryGetAccount(accountId, out var account))
            return false;

        if (account.CommandBudgetAccount && account.AccountPrototype != null)
        {
            var applied = false;
            var query = EntityQueryEnumerator<StationBankAccountComponent>();
            while (query.MoveNext(out var stationUid, out var stationBank))
            {
                if (!stationBank.Accounts.ContainsKey(account.AccountPrototype.Value))
                    continue;

                if (stationBank.Accounts[account.AccountPrototype.Value] + amount < 0)
                    return false;

                _cargo.UpdateBankAccount((stationUid, stationBank), amount, account.AccountPrototype.Value);
                applied = true;
                break;
            }

            if (!applied)
                return false;
        }
        else
        {
            if (account.Balance + amount < 0)
                return false;

            account.Balance += amount;
        }

        if (isTransfer)
        {
            AddHistory(account, amount, reason, initiator, counterpart);
        }
        else
        {
            if (amount > 0)
                _totalDeposited += amount;
            else
                _totalBurned += -amount;

            AddHistory(account, amount, reason, initiator, null);
        }

        if (account.CartridgeUid != null)
            _bankCartridge.UpdateUiState(account.CartridgeUid.Value);

        return true;
    }

    private void AddHistory(BankAccount account, int amount, TransactionReason reason, string? initiator, BankAccount? counterpart)
    {
        account.History ??= new List<TransactionsHistory>();

        string type;
        string who;
        string? targetId = null;

        if (counterpart != null)
        {
            type = Loc.GetString($"bank-reason-{reason.ToString().ToLowerInvariant()}");
            if (amount > 0)
                who = initiator ?? counterpart.Name;
            else
            {
                who = initiator ?? Loc.GetString("bank-system");
                targetId = counterpart.AccountId.ToString();
            }
        }
        else
        {
            type = amount > 0 ? Loc.GetString("bank-deposit") : Loc.GetString("bank-withdrawal");
            who = initiator ?? Loc.GetString("bank-system");
        }

        account.History.Add(new TransactionsHistory(
            amount,
            _timing.CurTime,
            type,
            who,
            targetId,
            Loc.GetString($"bank-reason-{reason.ToString().ToLowerInvariant()}")));

        if (account.History.Count > HistoryLimit)
            account.History.RemoveRange(0, account.History.Count - HistoryLimit);
    }

    public BankAccount CreateAccount(int accountId = default, int startingBalance = 0)
    {
        if (TryGetAccount(accountId, out var acc))
            return acc;

        BankAccount account;
        if (accountId == default)
        {
            int accountNumber;

            do
            {
                accountNumber = _random.Next(100000, 999999);
            } while (AccountExist(accountNumber));

            account = new BankAccount(accountNumber, startingBalance, _random);
        }
        else
        {
            account = new BankAccount(accountId, startingBalance, _random);
        }

        _accounts.Add(account);

        return account;
    }

    public bool AccountExist(int accountId)
    {
        return _accounts.Any(x => x.AccountId == accountId);
    }

    public bool TryGetAccount(int accountId, [NotNullWhen(true)] out BankAccount? account)
    {
        account = _accounts.FirstOrDefault(x => x.AccountId == accountId);
        return account != null;
    }

    public IReadOnlyList<BankAccount> GetAllAccounts() => _accounts;

    public int GetBalance(int accountId)
    {
        if (!TryGetAccount(accountId, out var account))
            return 0;

        if (account.CommandBudgetAccount && account.AccountPrototype != null)
        {
            var query = EntityQueryEnumerator<StationBankAccountComponent>();
            while (query.MoveNext(out var stationUid, out var stationBank))
            {
                if (stationBank.Accounts.TryGetValue(account.AccountPrototype.Value, out var balance))
                    return balance;
            }
            return 0;
        }

        return account.Balance;
    }
}
