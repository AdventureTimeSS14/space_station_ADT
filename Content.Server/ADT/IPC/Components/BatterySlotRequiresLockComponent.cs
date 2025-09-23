namespace Content.Server.ADT.Silicon.BatterySlot;

[RegisterComponent]
public sealed partial class BatterySlotRequiresLockComponent : Component
{
    [DataField]
    public string ItemSlot = string.Empty;
}
