using Content.Shared.ADT.Mime;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Mime;

public sealed class MimeFingerGunTrackerSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimeFingerGunItemComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnRemovedFromContainer(EntityUid uid, MimeFingerGunItemComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (!component.MimeUid.HasValue || !HasComp<HandsComponent>(component.MimeUid.Value))
        {
            QueueDel(uid);
            return;
        }

        if (!Exists(component.MimeUid.Value))
        {
            QueueDel(uid);
            return;
        }

        if (!Exists(uid))
            return;

        if (!IsInMimeHand(component.MimeUid.Value, uid))
        {
            QueueDel(uid);
        }
    }

    private bool IsInMimeHand(EntityUid mimeUid, EntityUid gunUid)
    {
        foreach (var handId in _sharedHandsSystem.EnumerateHands((mimeUid, null)))
        {
            if (_sharedHandsSystem.TryGetHeldItem((mimeUid, null), handId, out var held) && held == gunUid)
                return true;
        }

        return false;
    }
}
