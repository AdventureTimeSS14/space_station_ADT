using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public sealed class BankCartridgeUiState : BoundUserInterfaceState
{
    public int Balance;
    public int? AccountId = null;
    public string OwnerName = string.Empty;
    public string AccountLinkMessage = string.Empty;
    public string AccountLinkResult = string.Empty;
    public string TransferResult = string.Empty;
    public List<TransactionsHistory> History = new();
}

[Serializable, NetSerializable]
public sealed class BankAccountLinkMessage : CartridgeMessageEvent
{
    public int AccountId;
    public int Pin;

    public BankAccountLinkMessage(int accountId, int pin)
    {
        AccountId = accountId;
        Pin = pin;
    }
}

[Serializable, NetSerializable]
public sealed class BankTransferMessage : CartridgeMessageEvent
{
    public int AccountTargetId;
    public int Pin;
    public int Amount;

    public BankTransferMessage(int accountTargetId, int pin, int amount)
    {
        AccountTargetId = accountTargetId;
        Pin = pin;
        Amount = amount;
    }
}
