using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.Ranged.WearableGun;

public sealed class ADTWearableGunSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTWearableGunComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ADTWearableGunComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ADTWearableGunComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ADTWearableGunUserComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnEquipped(Entity<ADTWearableGunComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Slot != ent.Comp.Slot || !_net.IsServer)
            return;

        var user = EnsureComp<ADTWearableGunUserComponent>(args.Equipee);
        user.Gun = ent.Owner;
        Dirty(args.Equipee, user);
    }

    private void OnUnequipped(Entity<ADTWearableGunComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.Slot != ent.Comp.Slot)
            return;

        RemoveUser(ent.Owner, args.Equipee);
    }

    private void OnShutdown(Entity<ADTWearableGunComponent> ent, ref ComponentShutdown args)
    {
        if (Transform(ent.Owner).ParentUid is { Valid: true } parent)
            RemoveUser(ent.Owner, parent);
    }

    private void OnShotAttempted(Entity<ADTWearableGunUserComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.Gun == args.Used.Owner)
            return;

        ShowBlockPopup(ent);
        args.Cancel();
    }

    private void ShowBlockPopup(Entity<ADTWearableGunUserComponent> ent)
    {
        if (!TryComp<ADTWearableGunComponent>(ent.Comp.Gun, out var wearable))
            return;

        if (_timing.CurTime < ent.Comp.NextPopup)
            return;

        ent.Comp.NextPopup = _timing.CurTime + wearable.PopupCooldown;
        _popup.PopupClient(Loc.GetString(wearable.BlockPopup), ent.Owner, ent.Owner);
    }

    private void RemoveUser(EntityUid gun, EntityUid user)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<ADTWearableGunUserComponent>(user, out var comp))
            return;

        if (comp.Gun != gun)
            return;

        RemCompDeferred<ADTWearableGunUserComponent>(user);
    }
}
