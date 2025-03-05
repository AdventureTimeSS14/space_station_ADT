using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.Hands;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory.VirtualItem;

public abstract partial class SharedVirtualItemSystem
{
    public bool TryDeleteVirtualItem(Entity<VirtualItemComponent> item, EntityUid user)
    {
        var userEv = new BeforeVirtualItemDeletedEvent(item.Comp.BlockingEntity, user);
        RaiseLocalEvent(user, userEv);

        var targEv = new BeforeVirtualItemDeletedEvent(item.Comp.BlockingEntity, user);
        RaiseLocalEvent(item.Comp.BlockingEntity, targEv);

        if (userEv.Cancelled || targEv.Cancelled)
            return false;

        DeleteVirtualItem(item, user);
        return true;
    }
}
