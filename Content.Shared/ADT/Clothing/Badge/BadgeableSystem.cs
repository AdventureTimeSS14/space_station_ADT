using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.Clothing.Badge;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;

namespace Content.Shared.ADT.Clothing.Badge;

[Virtual]
public class BadgeableSystem : EntitySystem
{
    [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
    [Dependency] protected readonly BadgeSystem BadgeSystem = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] protected readonly ExamineSystemShared ExamineSystem = default!;

    override public void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<BadgeableComponent, ExaminedEvent>(HandleExaminedEvent);
        SubscribeLocalEvent<InventoryComponent, ExaminedEvent>(HandleEntityExaminedEvent);
    }

    public bool GetBadgeNumber(EntityUid entity, [NotNullWhen(true)] out string? badgeNumber, BadgeableComponent? component = null)
    {
        badgeNumber = null;

        if (!Resolve(entity, ref component))
            return false;

        var badge = ItemSlotsSystem.GetItemOrNull(entity, component.Slot);
        if (badge is null)
            return false;

        if (!TryComp<BadgeComponent>(badge, out var badgeComponent))
            return false;

        badgeNumber = badgeComponent.BadgeNumber;
        return true;
    }

    protected void HandleExaminedEvent(EntityUid entity, BadgeableComponent component, ExaminedEvent ev)
    {
        if (!GetBadgeNumber(entity, out var badgeNumber, component))
            return;

        if (!ExamineSystem.IsInDetailsRange(ev.Examiner, entity))
        {
            if (component.NotInDetailsText != String.Empty)
                ev.PushMarkup(Loc.GetString(component.NotInDetailsText, ("badgeNumber", badgeNumber)), -5);

            return;
        }

        if (component.InDetailsText != String.Empty)
            ev.PushMarkup(Loc.GetString(component.InDetailsText, ("badgeNumber", badgeNumber)), -5);
    }

    protected void HandleEntityExaminedEvent(EntityUid entity, InventoryComponent _, ExaminedEvent ev)
    {
        var enumerator = InventorySystem.GetSlotEnumerator(entity, SlotFlags.OUTERCLOTHING);
        while (enumerator.MoveNext(out var container))
        {
            if (!container.ContainedEntity.HasValue || container.ContainedEntity == null)
                continue;

            if (!TryComp<BadgeableComponent>(container.ContainedEntity, out var vest))
                continue;

            if (GetBadgeNumber(container.ContainedEntity.Value, out var badgeNumber))
            {
                if (ExamineSystem.IsInDetailsRange(ev.Examiner, entity))
                    ev.PushMarkup(Loc.GetString(vest.InDetailsText, ("badgeNumber", badgeNumber)), -5);
                else
                    ev.PushMarkup(Loc.GetString(vest.NotInDetailsText, ("badgeNumber", badgeNumber)), -5);
            }

        }
    }
}
