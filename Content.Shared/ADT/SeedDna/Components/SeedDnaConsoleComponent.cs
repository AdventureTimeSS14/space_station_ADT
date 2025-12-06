using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.SeedDna.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SeedDnaConsoleComponent : Component
{
    public static string SeedSlotId = "SeedSlotId";
    public static string DnaDiskSlotId = "DnaDiskSlotId";

    [DataField]
    public ItemSlot SeedSlot = new();

    [DataField]
    public ItemSlot DnaDiskSlot = new();
}
