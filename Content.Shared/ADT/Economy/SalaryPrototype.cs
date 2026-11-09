using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Economy;

[Prototype, DataDefinition]
public sealed partial class SalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("salaries")]
    public Dictionary<ProtoId<JobPrototype>, int> Salaries = new();

    // ЗП выдаётся из воздуха а не из депортамента (требуется для ОБР, и ЦК ролей такие как Щит и т.д.)
    [DataField("fixedSalaries")]
    public Dictionary<ProtoId<JobPrototype>, int> Fixed = new();
}
