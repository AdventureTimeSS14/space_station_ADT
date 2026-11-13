using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class ClothingCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public List<string> Slots = new();

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var inventory = entityManager.System<InventorySystem>();

        foreach (var slot in Slots)
        {
            if (inventory.TryGetSlotEntity(patient, slot, out _))
                return true;
        }

        return false;
    }
}
