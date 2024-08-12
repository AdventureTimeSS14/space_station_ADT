using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Economy;

[Prototype("salary")]
public sealed class SalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("salaries")]
    public Dictionary<string, int> Salaries = new();
}
