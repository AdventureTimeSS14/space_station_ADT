using Content.Server.Popups;
using Content.Shared.ADT.Mime;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mime;

public sealed class MimeFingerGunWeaponSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimeFingerGunItemComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<MimeFingerGunItemComponent, TakeAmmoEvent>(OnTakeAmmo, after: [typeof(SharedGunSystem)]);
    }

    private void OnTakeAmmo(EntityUid uid, MimeFingerGunItemComponent component, TakeAmmoEvent args)
    {
        if (!Exists(uid) || Terminating(uid))
            return;

        if (!TryComp<RevolverAmmoProviderComponent>(uid, out var revolver))
            return;

        var hasAmmo = false;
        foreach (var chamber in revolver.Chambers)
        {
            if (chamber == true)
            {
                hasAmmo = true;
                break;
            }
        }

        if (!hasAmmo)
        {
            Timer.Spawn(300, () =>
            {
                if (Exists(uid) && !Terminating(uid))
                {
                    var stillNoAmmo = true;
                    if (TryComp<RevolverAmmoProviderComponent>(uid, out var rev))
                    {
                        foreach (var chamber in rev.Chambers)
                        {
                            if (chamber == true)
                            {
                                stillNoAmmo = false;
                                break;
                            }
                        }
                    }

                    if (stillNoAmmo)
                    {
                        Del(uid);
                    }
                }
            });
        }
    }

    private void OnGunShot(EntityUid uid, MimeFingerGunItemComponent component, GunShotEvent args)
    {
        if (!Exists(uid) || Terminating(uid))
            return;

        ShowFingerGunShotEmote(uid, args.User);
    }

    private void ShowFingerGunShotEmote(EntityUid uid, EntityUid user)
    {
        var soundVariants = new[]
        {
            Loc.GetString("mime-finger-gun-popup-1"),
            Loc.GetString("mime-finger-gun-popup-2"),
            Loc.GetString("mime-finger-gun-popup-3"),
            Loc.GetString("mime-finger-gun-popup-4")
        };
        var randomSound = _random.Pick(soundVariants);
        _popupSystem.PopupEntity(randomSound, uid, user);
    }
}
