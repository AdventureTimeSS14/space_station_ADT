using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class MachinePartStorageComponent : Component
{
    [DataField]
    public List<MachinePartSlot> Parts = new();
}

[DataDefinition, Serializable]
public partial struct MachinePartSlot
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> Part;

    [DataField]
    public int Tier = 1;

    [DataField]
    public int Quantity = 1;
}
