using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class MachinePartStorageComponent : Component
{
    [DataField]
    public List<MachinePartSlot> Parts = new();
    public void AddSlot(ProtoId<MachinePartPrototype> part, int tier, int quantity)
    {
        for (var i = 0; i < Parts.Count; i++)
        {
            var slot = Parts[i];
            if (slot.Part != part || slot.Tier != tier)
                continue;

            Parts[i] = slot with { Quantity = slot.Quantity + quantity };
            return;
        }

        Parts.Add(new MachinePartSlot { Part = part, Tier = tier, Quantity = quantity });
    }
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
