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
using Content.Server.Station.Events;
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
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Server.ADT.Traits;
using Content.Shared.ADT.Roles;
using Content.Shared.Database;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

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
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;

    private SalaryPrototype _salaries = default!;
    private readonly List<BankAccount> _accounts = new();
    private readonly Dictionary<string, ProtoId<CargoAccountPrototype>> _jobDepartments = new();
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
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);

        _protoMan.PrototypesReloaded += OnPrototypesReloaded;
        BuildJobDepartmentMap();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _protoMan.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.ByType.ContainsKey(typeof(DepartmentPrototype)) ||
            args.ByType.ContainsKey(typeof(CargoAccountPrototype)))
            BuildJobDepartmentMap();
    }

    private void BuildJobDepartmentMap()
    {
        _jobDepartments.Clear();
        foreach (var department in _protoMan.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!_protoMan.HasIndex<CargoAccountPrototype>(department.ID) &&
                !(department.ID == "Civilian" && _protoMan.HasIndex<CargoAccountPrototype>("Service")))
                continue;

            ProtoId<CargoAccountPrototype> account = department.ID == "Civilian"
                ? "Service"
                : department.ID;

            foreach (var role in department.Roles)
            {
                if (!_jobDepartments.ContainsKey(role))
                    _jobDepartments[role] = account;
            }
        }
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

        var delay = Math.Max(1f, _configManager.GetCVar(ADTCCVars.SalaryDelay));
        if (_salaryTimer <= delay)
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

        var groups = new Dictionary<int, SalaryGroup>();
        foreach (var account in _accounts.Where(account =>
                     !account.CommandBudgetAccount &&
                     account.Mind is { Comp.UserId: not null, Comp.CurrentEntity: not null } &&
                     _playerManager.TryGetSessionById(account.Mind.Value.Comp.UserId!.Value, out _) &&
                     !_mobState.IsDead(account.Mind.Value.Comp.CurrentEntity!.Value)))
        {
            var (isFixed, pay) = GetPay(account.Mind);
            if (pay <= 0 || account.Mind!.Value.Comp.CurrentEntity is not { } mob)
                continue;

            if (!_job.MindTryGetJob(account.Mind, out var job))
                continue;

            if (isFixed)
            {
                if (TryDeposit(account.AccountId, pay, TransactionReason.Salary, Loc.GetString("bank-initiator-centcom")))
                    _totalSalary += pay;
                continue;
            }

            EntityUid? homeStation = account.Station != null &&
                                     TryComp<StationBankAccountComponent>(account.Station, out _)
                ? account.Station
                : _station.GetOwningStation(mob);

            if (homeStation is not { } station ||
                !TryComp<StationBankAccountComponent>(station, out var stationBank))
            {
                if (TryDeposit(account.AccountId, pay, TransactionReason.Salary, Loc.GetString("bank-initiator-centcom")))
                    _totalSalary += pay;
                continue;
            }

            var fundingProto = GetDepartmentForJob(job.ID) ?? stationBank.PrimaryAccount;
            if (!TryGetDepartmentAccountId(station, fundingProto, out var fundingId))
                continue;

            if (!groups.TryGetValue(fundingId, out var group))
            {
                group = new SalaryGroup(station, fundingProto);
                groups[fundingId] = group;
            }

            group.Members.Add((account.AccountId, pay, job.ID));
        }

        foreach (var (fundingId, group) in groups)
        {
            var primaryId = 0;
            if (TryComp<StationBankAccountComponent>(group.Station, out var stationBank) &&
                TryGetDepartmentAccountId(group.Station, stationBank.PrimaryAccount, out var primaryAccountId))
                primaryId = primaryAccountId;

            var cut = 0f;
            if (TryComp<StationSalaryCutComponent>(group.Station, out var salaryCuts) &&
                salaryCuts.Cuts.TryGetValue(group.Funding, out var fundingCut))
                cut = Math.Clamp(fundingCut, 0f, salaryCuts.MaxCut);

            var remaining = GetBalance(fundingId);

            foreach (var (playerAccountId, pay, jobId) in group.Members)
            {
                var gross = (int) Math.Round(pay * (1 + GetSalaryBonus(group.Station, jobId)));
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
                }
                else
                {
                    var taxPart = taxRate > 0 && primaryId != 0 && fundingId != primaryId
                        ? (int) Math.Floor(affordable * taxRate)
                        : 0;
                    var payout = affordable - taxPart;

                    if (taxPart > 0 && TryTransfer(fundingId, primaryId, taxPart, TransactionReason.SalaryTax))
                        _totalTaxes += taxPart;

                    if (payout > 0 && TryTransfer(fundingId, playerAccountId, payout, TransactionReason.Salary))
                        _totalSalary += payout;
                }

                remaining -= affordable;
            }
        }

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("salary-pay-announcement"),
            colorOverride: Color.FromHex("#18abf5"));
    }

    private sealed class SalaryGroup(EntityUid station, ProtoId<CargoAccountPrototype> funding)
    {
        public readonly EntityUid Station = station;
        public readonly ProtoId<CargoAccountPrototype> Funding = funding;

        public readonly List<(int PlayerAccount, int Pay, string JobId)> Members = new();
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

    private ProtoId<CargoAccountPrototype>? GetDepartmentForJob(string jobId)
    {
        if (_jobDepartments.TryGetValue(jobId, out var department))
            return department;

        return null;
    }

    public ProtoId<JobPrototype>? GetCardJob(EntityUid user)
    {
        if (!_idCardSystem.TryFindIdCard(user, out var card))
            return null;

        if (card.Comp.JobPrototype != null)
            return card.Comp.JobPrototype;

        if (TryComp<StationRecordKeyStorageComponent>(card, out var keyStorage) &&
            keyStorage.Key is { } key &&
            _records.TryGetRecord<GeneralStationRecord>(key, out var record) &&
            record.JobPrototype.Length > 0)
            return record.JobPrototype;

        if (TryComp<RoleCacheComponent>(user, out var roleCache) &&
            roleCache.LastJobPrototype != null)
            return roleCache.LastJobPrototype;

        return null;
    }

    public ProtoId<CargoAccountPrototype>? GetCardDepartment(EntityUid user)
    {
        var job = GetCardJob(user);
        return job == null ? null : GetDepartmentForJob(job.Value);
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

    public bool TryGetPersonalAccountId(EntityUid user, out int accountId)
    {
        accountId = 0;
        var fallbackFound = false;

        foreach (var item in _accessReader.FindPotentialAccessItems(user))
        {
            BankCardComponent? card = null;

            if (TryComp(item, out PdaComponent? pda) &&
                pda.ContainedId is { Valid: true } id)
                TryComp(id, out card);

            if (card == null)
                TryComp(item, out card);

            if (card?.AccountId is not { } account)
                continue;

            if (!card.CommandBudgetCard)
            {
                accountId = account;
                return true;
            }

            if (!fallbackFound)
            {
                accountId = account;
                fallbackFound = true;
            }
        }

        return fallbackFound;
    }

    private (bool Fixed, int Pay) GetPay(EntityUid? mind)
    {
        if (!_job.MindTryGetJob(mind, out var job))
            return (false, 0);

        if (_salaries.Fixed.TryGetValue(job.ID, out var fixedPay))
            return (true, fixedPay);

        return (_salaries.Salaries.TryGetValue(job.ID, out var pay), pay);
    }

    private void OnMapInit(EntityUid uid, BankCardComponent component, MapInitEvent args)
    {
        if (component.CommandBudgetCard)
        {
            if (_station.GetOwningStation(uid) is { } station &&
                component.CommandBudgetType != null &&
                TryGetDepartmentAccountId(station, component.CommandBudgetType.Value, out var accountId))
            {
                component.AccountId = accountId;
            }

            return;
        }

        if (component.AccountId.HasValue)
        {
            CreateAccount(component.AccountId.Value, component.StartingBalance);
            return;
        }

        var playerAccount = CreateAccount(default, component.StartingBalance);
        component.AccountId = playerAccount.AccountId;
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        var query = EntityQueryEnumerator<BankCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (!card.CommandBudgetCard || card.AccountId.HasValue || card.CommandBudgetType == null)
                continue;

            if (_station.GetOwningStation(uid) != ev.Station)
                continue;

            if (TryGetDepartmentAccountId(ev.Station, card.CommandBudgetType.Value, out var accountId))
                card.AccountId = accountId;
        }
    }

    public bool TryGetDepartmentAccountId(EntityUid station, ProtoId<CargoAccountPrototype> proto, out int accountId)
    {
        accountId = 0;

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return false;

        if (!bank.BankAccounts.TryGetValue(proto, out var account))
        {
            account = CreateDepartmentAccount(station, proto);
            bank.BankAccounts[proto] = account;

            _cargo.TryAdjustBankAccount((station, bank), proto, 0, true);
        }

        accountId = account.AccountId;
        return true;
    }

    private BankAccount CreateDepartmentAccount(EntityUid station, ProtoId<CargoAccountPrototype> departmentType)
    {
        int accountNumber;
        do
        {
            accountNumber = _random.Next(100000, 999999);
        } while (AccountExist(accountNumber));

        var account = new BankAccount(accountNumber, 0, _random);
        account.AccountPrototype = departmentType;
        account.CommandBudgetAccount = true;
        account.Station = station;
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

            var (_, pay) = GetPay(mind.Mind);
            var startingBalance = 100 + pay;

            if (TryComp<StartingBalanceComponent>(ev.Mob, out var balanceComp))
                startingBalance = balanceComp.Balance;

            bankAccount.Balance = startingBalance;
            mindComponent.AddMemory(new Memory("PIN", bankAccount.AccountPin.ToString()));
            mindComponent.AddMemory(new Memory(Loc.GetString("character-info-memories-account-number"),
                bankAccount.AccountId.ToString()));
            bankAccount.Mind = (mind.Mind.Value, mindComponent);
            bankAccount.Name = Name(ev.Mob);
            bankAccount.Station = ev.Station.IsValid() ? ev.Station : null;

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
            if (!Mutate(fromAccountId, amount, reason, initiator, isTransfer: false, trackStats: false))
                Log.Error($"[Economy] refund of {amount} to account {fromAccountId} failed after failed transfer to {toAccountId}");

            return false;
        }

        _totalTransfers += amount;
        return true;
    }

    private bool Mutate(int accountId, int amount, TransactionReason reason, string? initiator, bool isTransfer,
        BankAccount? counterpart = null, bool trackStats = true)
    {
        if (amount == 0 || !TryGetAccount(accountId, out var account))
            return false;

        if (account.CommandBudgetAccount && account.AccountPrototype != null)
        {
            if (account.Station == null ||
                !TryComp<StationBankAccountComponent>(account.Station, out var ownBank) ||
                !ownBank.Accounts.ContainsKey(account.AccountPrototype.Value))
                return false;

            if (ownBank.Accounts[account.AccountPrototype.Value] + amount < 0)
                return false;

            _cargo.UpdateBankAccount((account.Station.Value, ownBank), amount, account.AccountPrototype.Value);
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
            if (trackStats)
            {
                if (amount > 0)
                    _totalDeposited += amount;
                else
                    _totalBurned += -amount;
            }

            AddHistory(account, amount, reason, initiator, null);
        }

        if (account.CartridgeUid != null)
            _bankCartridge.UpdateUiState(account.CartridgeUid.Value);

        return true;
    }

    private void AddHistory(BankAccount account, int amount, TransactionReason reason, string? initiator, BankAccount? counterpart)
    {
        account.History ??= new List<TransactionsHistory>();

        if (!Loc.TryGetString($"bank-reason-{reason.ToString().ToLowerInvariant()}", out var reasonText))
            reasonText = reason.ToString();

        string type;
        string who;
        string? targetId = null;

        if (counterpart != null)
        {
            type = reasonText;
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
            reasonText));

        var limit = Math.Max(0, _configManager.GetCVar(ADTCCVars.BankHistoryLimit));
        if (account.History.Count > limit)
            account.History.RemoveRange(0, account.History.Count - limit);
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
            if (account.Station != null &&
                TryComp<StationBankAccountComponent>(account.Station, out var ownBank) &&
                ownBank.Accounts.TryGetValue(account.AccountPrototype.Value, out var ownBalance))
                return ownBalance;

            return 0;
        }

        return account.Balance;
    }
}
