using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Economy;

[Prototype, DataDefinition]
public sealed partial class SalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("salaries")]
    public Dictionary<ProtoId<JobPrototype>, double> Salaries = new();

    [DataField("fixedSalaries")]
    public Dictionary<ProtoId<JobPrototype>, int> Fixed = new();
}
