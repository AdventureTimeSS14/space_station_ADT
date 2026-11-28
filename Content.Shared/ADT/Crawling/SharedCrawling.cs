using Content.Shared.Inventory;

namespace Content.Shared.ADT.Crawling;

[ByRefEvent]
public record struct ExplosionDownAttemptEvent(string Explosion, bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
