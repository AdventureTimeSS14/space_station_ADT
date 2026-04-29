using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Economy;

[Prototype("salary"), DataDefinition]
public sealed partial class SalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("salaries")]
    public Dictionary<string, int> Salaries = new();
}
