using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public enum TransactionReason : byte
{
    Other = 0,
    Salary,
    SalaryTax,
    VendingSale,
    AtmWithdraw,
    AtmDeposit,
    EftposPayment,
    BankTransfer,
    PredictorFee,
    AdminAction,
    RevConfiscation,
}
