using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public sealed class TransactionsHistory
{
    public int Amount;
    public TimeSpan Timestamp;
    public string Type;
    public string InitiatorName;

    public string? TargetId;

    public string? Reason;

    public TransactionsHistory(int amount, TimeSpan timestamp, string type, string initiatorName, string? targetId, string? reason = null)
    {
        Amount = amount;
        Timestamp = timestamp;
        Type = type;
        InitiatorName = initiatorName;
        TargetId = targetId;
        Reason = reason;
    }
}
