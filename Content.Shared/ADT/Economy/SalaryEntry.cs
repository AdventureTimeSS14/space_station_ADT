using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public enum SalaryType
{
    Personal,
    JobGroup,
    Department
}

[Serializable, NetSerializable]
public enum PaymentMode
{
    Manual,
    Automatic
}

[Serializable, NetSerializable]
public sealed class SalaryEntry
{
    public SalaryType Type;
    public int? AccountId;
    public string? JobGroup;
    public string? Department;
    public int Amount;

    public PaymentMode Mode;
    public TimeSpan Interval;
    public TimeSpan NextPaymentTime;

    public bool IsActive = true;
}
