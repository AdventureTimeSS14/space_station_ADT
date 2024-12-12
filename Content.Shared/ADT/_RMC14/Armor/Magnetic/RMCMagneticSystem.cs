﻿using Content.Shared._RMC14.Inventory;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Throwing;

namespace Content.Shared._RMC14.Armor.Magnetic;

public sealed class RMCMagneticSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMagneticItemComponent, DroppedEvent>(OnMagneticItemDropped);
        SubscribeLocalEvent<RMCMagneticItemComponent, RMCDroppedEvent>(OnMagneticItemRMCDropped);
        // SubscribeLocalEvent<RMCMagneticItemComponent, ThrownEvent>(OnMagneticItemThrown);
        SubscribeLocalEvent<RMCMagneticItemComponent, DropAttemptEvent>(OnMagneticItemDropAttempt);
    }

    private void OnMagneticItemDropped(Entity<RMCMagneticItemComponent> ent, ref DroppedEvent args)
    {
        TryReturn(ent, args.User);
    }

    private void OnMagneticItemRMCDropped(Entity<RMCMagneticItemComponent> ent, ref RMCDroppedEvent args)
    {
        TryReturn(ent, args.User);
    }

    // private void OnMagneticItemThrown(Entity<RMCMagneticItemComponent> ent, ref ThrownEvent args)
    // {
    //     if (args.User is not { } user)
    //         return;

    //     if (!TryReturn(ent, user))
    //         return;

    //     if (TryComp(ent, out ThrownItemComponent? thrown))
    //         _thrownItem.StopThrow(ent, thrown);
    // }

    private void OnMagneticItemDropAttempt(Entity<RMCMagneticItemComponent> ent, ref DropAttemptEvent args)
    {
        args.Cancel();
    }

    private bool TryReturn(Entity<RMCMagneticItemComponent> ent, EntityUid user)
    {
        var returnComp = EnsureComp<RMCReturnToInventoryComponent>(ent);
        returnComp.User = user;

        Dirty(ent, returnComp);
        return true;
    }

    public void SetMagnetizeToSlots(Entity<RMCMagneticItemComponent> ent, SlotFlags slots)
    {
        ent.Comp.MagnetizeToSlots = slots;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCReturnToInventoryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Returned)
                continue;

            var user = comp.User;
            if (!TerminatingOrDeleted(user))
            {
                var slots = _inventory.GetSlotEnumerator(user, SlotFlags.SUITSTORAGE);
                while (slots.MoveNext(out var slot))
                {
                    if (_inventory.TryGetSlotEntity(user, "outerClothing", out _))
                    {
                        if (_inventory.TryEquip(user, uid, slot.ID, force: false))
                        {
                            var popup = Loc.GetString("rmc-magnetize-return",
                                ("item", uid),
                                ("user", user));
                            _popup.PopupClient(popup, user, user, PopupType.Medium);

                            comp.Returned = true;
                            Dirty(uid, comp);
                            break;
                        }
                    }
                }
            }

            RemCompDeferred<RMCReturnToInventoryComponent>(uid);
        }
    }
}
