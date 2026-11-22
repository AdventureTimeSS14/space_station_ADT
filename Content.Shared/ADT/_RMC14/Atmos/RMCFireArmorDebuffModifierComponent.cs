// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFireArmorDebuffModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DebuffModifier = 1;
}

[ByRefEvent]
public record struct RMCGetFireArmorDebuffEvent(float Modifier = 1) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
