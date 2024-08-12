using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public sealed class EftposBuiState : BoundUserInterfaceState
{
    public bool Locked;
    public int Amount;
    public string Owner = string.Empty;
}

[Serializable, NetSerializable]
public sealed class EftposLockMessage : BoundUserInterfaceMessage
{
    public int Amount;

    public EftposLockMessage(int amount)
    {
        Amount = amount;
    }
}
