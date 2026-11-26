using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.ADT.Economy;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.ADT.Economy;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

internal static class AdtBankCommandUtils
{
    internal static bool TryParseDepartment(IPrototypeManager protoMan, string arg, out ProtoId<CargoAccountPrototype> department)
    {
        foreach (var proto in protoMan.EnumeratePrototypes<CargoAccountPrototype>())
        {
            if (proto.ID.Equals(arg, StringComparison.OrdinalIgnoreCase))
            {
                department = proto.ID;
                return true;
            }
        }

        department = default;
        return false;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class AdtBankPersonalCommand : IConsoleCommand
{
    public string Command => "bankpersonal";
    public string Description => "Список личных банковских счетов экипажа.";
    public string Help => "bankpersonal";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Help);
            return;
        }

        var bank = IoCManager.Resolve<IEntityManager>().System<BankCardSystem>();

        foreach (var account in bank.GetAllAccounts())
        {
            if (account.CommandBudgetAccount)
                continue;

            var name = string.IsNullOrEmpty(account.Name) ? account.AccountId.ToString() : account.Name;
            shell.WriteLine($"{account.AccountId}  {name} — {bank.GetBalance(account.AccountId)}$");
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class AdtBankAdjustCommand : IConsoleCommand
{
    public string Command => "bankadjust";
    public string Description => "Пополнить или опустошить личный счёт по номеру (отрицательная сумма — списание).";
    public string Help => "bankadjust <номер счёта> <сумма>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2 || !int.TryParse(args[0], out var accountId) || !int.TryParse(args[1], out var amount))
        {
            shell.WriteLine(Help);
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var bank = entManager.System<BankCardSystem>();
        var logger = IoCManager.Resolve<IAdminLogManager>();

        if (!bank.TryGetAccount(accountId, out var account))
        {
            shell.WriteLine($"Счёт {accountId} не найден.");
            return;
        }

        if (amount > 0)
        {
            if (!bank.TryDeposit(accountId, amount, TransactionReason.AdminAction))
            {
                shell.WriteLine($"Не удалось пополнить счёт {accountId} ({account.Name}): текущий баланс {bank.GetBalance(accountId)}$.");
                return;
            }
        }
        else if (amount < 0 && !bank.TryBurn(accountId, -amount, TransactionReason.AdminAction))
        {
            shell.WriteLine($"Не удалось списать {Math.Abs(amount)}$ со счёта {accountId} ({account.Name}): текущий баланс {bank.GetBalance(accountId)}$.");
            return;
        }

        logger.Add(LogType.Economy, LogImpact.Medium,
            $"[Economy] admin adjusted account {accountId} ({account.Name}) by {amount}$, new balance {bank.GetBalance(accountId)}$");
        shell.WriteLine($"Счёт {accountId} ({account.Name}): {bank.GetBalance(accountId)}$");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class AdtBankBonusCommand : IConsoleCommand
{
    private const int MaxSalaryBonusPercent = 500;

    public string Command => "bankbonus";
    public string Description => "Надбавка к зарплате отдела в процентах (влияет на все станции).";
    public string Help => "bankbonus <отдел: Cargo/Engineering/Medical/Science/Security/Service> <проценты>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2 || !double.TryParse(args[1], out var percent) || !double.IsFinite(percent))
        {
            shell.WriteLine(Help);
            return;
        }

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (!AdtBankCommandUtils.TryParseDepartment(protoMan, args[0], out var department))
        {
            shell.WriteLine($"Неизвестный отдел: {args[0]}.");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var logger = IoCManager.Resolve<IAdminLogManager>();
        var stations = 0;

        var query = entManager.EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var station, out var bank))
        {
            bank.SalaryBonus[department] = Math.Clamp(percent, 0, MaxSalaryBonusPercent) / 100.0;
            entManager.Dirty(station, bank);
            stations++;

            logger.Add(LogType.Economy, LogImpact.Medium,
                $"[Economy] salary bonus for {department} on {entManager.ToPrettyString(station)} set to {bank.SalaryBonus[department]:P0}");
        }

        shell.WriteLine(stations > 0
            ? $"Отдел {department}: надбавка +{percent:0}% (станций: {stations})."
            : "Станций с банковским счётом не найдено.");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class AdtBankCutCommand : IConsoleCommand
{
    public string Command => "bankcut";
    public string Description => "Удержание из зарплаты отдела в процентах. 0 - снять удержание. Без аргументов - список активных удержаний.";
    public string Help => "bankcut <отдел: Cargo/Engineering/Medical/Science/Security/Service> <проценты 0-90> | bankcut";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var cutSystem = entManager.System<StationSalaryCutSystem>();

        if (args.Length == 0)
        {
            var any = false;
            var cutsQuery = entManager.EntityQueryEnumerator<StationSalaryCutComponent>();
            while (cutsQuery.MoveNext(out var station, out var cuts))
            {
                foreach (var (dept, cut) in cuts.Cuts)
                {
                    any = true;
                    shell.WriteLine($"{entManager.ToPrettyString(station)}: {dept} — {cut:P0}");
                }
            }

            if (!any)
                shell.WriteLine("Активных удержаний нет.");

            return;
        }

        if (args.Length != 2 || !double.TryParse(args[1], out var percent) || !double.IsFinite(percent))
        {
            shell.WriteLine(Help);
            return;
        }

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (!AdtBankCommandUtils.TryParseDepartment(protoMan, args[0], out var department))
        {
            shell.WriteLine($"Неизвестный отдел: {args[0]}.");
            return;
        }

        var logger = IoCManager.Resolve<IAdminLogManager>();
        var stations = 0;
        float applied = 0;

        var query = entManager.EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var station, out var bank))
        {
            applied = cutSystem.SetCut(station, department, (float) percent / 100f);
            stations++;

            logger.Add(LogType.Economy, LogImpact.Medium,
                $"[Economy] salary cut for {department} on {entManager.ToPrettyString(station)} set to {applied:P0}");
        }

        shell.WriteLine(stations > 0
            ? $"Отдел {department}: удержание {applied:P0} (станций: {stations})."
            : "Станций с банковским счётом не найдено.");
    }
}
