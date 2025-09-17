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
    public List<BankTransaction> TransactionHistory = new();
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
public sealed class BankTabSwitchMessage : CartridgeMessageEvent
{
    public BankTab Tab;

    public BankTabSwitchMessage(BankTab tab)
    {
        Tab = tab;
    }
}

[Serializable, NetSerializable]
public enum BankTab : byte
{
    Account = 0,
    History = 1
}
