using Content.Shared.Whitelist;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent]
public sealed partial class MechEquipmentLimitComponent : Component
{
    [DataField(required: true)]
    public List<MechEquipmentLimit> Limits = new();
}

[DataDefinition]
public sealed partial class MechEquipmentLimit
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField]
    public int Max = 1;

    [DataField]
    public LocId Popup = "adt-mech-equipment-slot-occupied";
}
