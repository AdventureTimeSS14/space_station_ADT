using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankTransactionHistoryComponent : Component
{
    [DataField]
    public List<BankTransaction> Transactions = new();

    [DataField]
    public int MaxTransactions = 100;
}

[Serializable, NetSerializable]
public sealed record BankTransaction
{
    public TimeSpan Timestamp { get; init; }

    public BankTransactionType Type { get; init; }

    public int Amount { get; init; }

    public int BalanceAfter { get; init; }

    /// <summary>
    /// Описание транзакции
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public string? Details { get; init; }
}

[Serializable, NetSerializable]
public enum BankTransactionType : byte
{
    Unknown = 0,
    AtmDeposit = 1,
    AtmWithdraw = 2,
    Purchase = 3,
    Transfer = 4,
    Salary = 5,
    EftposPayment = 6,
}
