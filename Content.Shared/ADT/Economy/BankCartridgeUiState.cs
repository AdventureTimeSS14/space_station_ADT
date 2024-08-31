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
