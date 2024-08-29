using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public sealed class ATMRequestWithdrawMessage : BoundUserInterfaceMessage
{
    public int Amount;
    public int Pin;

    public ATMRequestWithdrawMessage(int amount, int pin)
    {
        Amount = amount;
        Pin = pin;
    }
}

[Serializable, NetSerializable]
public sealed class ATMBuiState : BoundUserInterfaceState
{
    public bool HasCard;
    public string InfoMessage = string.Empty;
    public int AccountBalance;
}
