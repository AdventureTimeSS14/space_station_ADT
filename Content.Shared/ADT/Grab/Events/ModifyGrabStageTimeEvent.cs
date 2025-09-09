using Content.Shared.Inventory;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Shared.ADT.Grab;

[ByRefEvent]
public record struct ModifyGrabStageTimeEvent(GrabStage Stage, float Modifier = 1f) : IInventoryRelayEvent
{
    public bool Cancelled = false;
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
