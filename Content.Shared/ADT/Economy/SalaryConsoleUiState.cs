using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public sealed class SalaryConsoleUiState : BoundUserInterfaceState
{
    public int Balance;
    public List<SalaryEntry> SalaryEntries = new();
    public List<EmployeeInfo> JobList = new();
}

[Serializable, NetSerializable]
public sealed class EmployeeInfo
{
    public int AccountId;
    public string? Name;
    public string? JobId;
    public string? JobTitle;
    public string? Department;

}

[Serializable, NetSerializable]
public sealed class AddSalaryEntryMessage : BoundUserInterfaceMessage
{
    public SalaryEntry Entry;

    public AddSalaryEntryMessage(SalaryEntry entry)
    {
        Entry = entry;
    }
}

[Serializable, NetSerializable]
public sealed class RemoveSalaryEntryMessage : BoundUserInterfaceMessage
{
    public int Index;

    public RemoveSalaryEntryMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class PaySalariesMessage : BoundUserInterfaceMessage
{
}
