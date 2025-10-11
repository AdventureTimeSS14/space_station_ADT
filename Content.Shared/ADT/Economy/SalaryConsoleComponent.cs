using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[RegisterComponent]
public sealed partial class SalaryConsoleComponent : Component
{
    [DataField("salaryEntries")]
    public List<SalaryEntry> SalaryEntries = new();
}


[Serializable, NetSerializable]
public enum SalaryConsoleUiKey
{
    Key
}
