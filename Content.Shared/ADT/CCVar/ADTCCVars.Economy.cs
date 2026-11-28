using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

public sealed partial class ADTCCVars
{
    public static readonly CVarDef<bool> PaySalary =
        CVarDef.Create("economy.pay_salary", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Доля зарплаты, уходящая как взнос на содержание станции в счёт PrimaryAccount.
    /// 0.15 = 15% зарплаты идёт в бюджет станции. 0 отключает налог.
    /// </summary>
    public static readonly CVarDef<float> SalaryTaxRate =
        CVarDef.Create("economy.salary_tax_rate", 0.20f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Рыночный перегрев - перезакупленный товар дешевеет в приёмке на паллетах.
    /// </summary>
    public static readonly CVarDef<bool> MarketDeflation =
        CVarDef.Create("economy.market_deflation", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Минимальный депозит ЦК на каждый департаментский счёт каждую выплату зарплаты.
    /// </summary>
    public static readonly CVarDef<int> DepartmentDeposit =
        CVarDef.Create("economy.department_deposit", 2000, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Интервал выплат зарплат
    /// </summary>
    public static readonly CVarDef<int> SalaryDelay =
        CVarDef.Create("economy.salary_delay", 2700, CVar.SERVERONLY);

    /// <summary>
    /// Максимальное количество записей в истории транзакций счёта
    /// </summary>
    public static readonly CVarDef<int> BankHistoryLimit =
        CVarDef.Create("economy.bank_history_limit", 100, CVar.SERVERONLY);

    /// <summary>
    /// Насыщение цены газа: до этого числа молей цена линейная, дальше -
    /// логарифмическая убывающая отдача. Базовое значение 0 - ничего резаться не будет.
    /// </summary>
    public static readonly CVarDef<float> GasPriceSaturationMoles =
        CVarDef.Create("economy.gas_price_saturation_moles", 50f, CVar.SERVERONLY);
}
