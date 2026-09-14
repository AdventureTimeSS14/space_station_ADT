using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.VendingMachines;

[Prototype]
public sealed partial class VendingMachineCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; } = string.Empty;

    [DataField]
    public EntProtoId Icon { get; private set; } = string.Empty;

    [DataField]
    public Color AccentColor = Color.FromHex("#4a9eff");

    [DataField]
    public int Priority;
}