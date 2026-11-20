using Content.Shared.Actions.Events;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Chaplain.Systems;

public sealed class HolyRelicSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RelicRequiredActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(Entity<RelicRequiredActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || HasRelic(args.User))
            return;

        args.Cancelled = true;
        args.Reason = Loc.GetString("chaplain-relic-required");
        args.Type = PopupType.MediumCaution;
    }

    public bool HasRelic(EntityUid uid)
    {
        if (!TryComp<RelicBoundComponent>(uid, out var bound))
            return true;

        return _inventory.TryGetSlotEntity(uid, bound.Slot, out var relic) && HasComp<HolyRelicComponent>(relic);
    }
}
