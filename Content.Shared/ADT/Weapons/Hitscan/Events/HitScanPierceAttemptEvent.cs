using Content.Shared.ADT.Combat.Ranged.Pierce;
using Content.Shared.Inventory;

namespace Content.Shared.ADT.Weapons.Hitscan.Events;

[ByRefEvent]
public record struct HitScanPierceAttemptEvent(PierceLevel Level, bool Pierced) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
