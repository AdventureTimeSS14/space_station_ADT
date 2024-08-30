using Content.Shared.Inventory;

namespace Content.Shared.Chemistry;

public sealed class InjectAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    // Whenever locational damage is a thing, this should just check only that bit of armour.
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public InjectAttemptEvent()
    {
    }
}
