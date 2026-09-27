using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Inventory;

public sealed class ADTBackDrawSystem : EntitySystem
{
    private static readonly string[] BackSlots = { "suitstorage", "back" };

    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.DrawBackItem,
                InputCmdHandler.FromDelegate(HandleDraw, handle: false, outsidePrediction: false))
            .Register<ADTBackDrawSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<ADTBackDrawSystem>();
    }

    private void HandleDraw(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } uid || !Exists(uid))
            return;

        if (!_blocker.CanInteract(uid, null))
            return;

        if (!TryComp<HandsComponent>(uid, out var hands) || hands.ActiveHandId is null)
            return;

        if (_hands.GetActiveItem((uid, hands)) is { } held)
        {
            Stow(uid, hands, held);
            return;
        }

        Draw(uid, hands);
    }

    private void Draw(EntityUid uid, HandsComponent hands)
    {
        foreach (var slot in BackSlots)
        {
            if (!_inventory.TryGetSlotEntity(uid, slot, out var item))
                continue;

            if (HasComp<StorageComponent>(item))
                continue;

            if (!_inventory.CanUnequip(uid, slot, out var reason))
            {
                _popup.PopupClient(Loc.GetString(reason), uid, uid);
                return;
            }

            if (!_inventory.TryUnequip(uid, slot, predicted: true, checkDoafter: true))
                return;

            if (!_hands.TryPickup(uid, item.Value, handsComp: hands))
                _inventory.TryEquip(uid, item.Value, slot, predicted: true, checkDoafter: true);

            return;
        }

        _popup.PopupClient(Loc.GetString("adt-back-draw-empty"), uid, uid);
    }

    private void Stow(EntityUid uid, HandsComponent hands, EntityUid held)
    {
        foreach (var slot in BackSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                continue;

            if (!_inventory.CanEquip(uid, held, slot, out _))
                continue;

            _hands.TryDrop((uid, hands), hands.ActiveHandId!);
            _inventory.TryEquip(uid, held, slot, predicted: true, checkDoafter: true);
            return;
        }

        _popup.PopupClient(Loc.GetString("adt-back-draw-no-space"), uid, uid);
    }
}
