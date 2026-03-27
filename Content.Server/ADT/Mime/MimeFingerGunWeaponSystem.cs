using Content.Server.Popups;
using Content.Shared.ADT.Mime;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mime;

public sealed class MimeFingerGunWeaponSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimeFingerGunItemComponent, GunShotEvent>(OnGunShot);
    }

    private void OnGunShot(EntityUid uid, MimeFingerGunItemComponent component, GunShotEvent args)
    {
        if (!Exists(uid) || Terminating(uid))
            return;

        if (component.RemainingShots <= 0)
            return;

        ShowFingerGunShotEmote(uid, args.User);

        component.RemainingShots--;
        Dirty(uid, component);

        // Не удаляем пистолет сразу, а просто уменьшаем счётчик
        // Это предотвращает ошибку PVS с летящей пулей, т.к я не знаю как заставить
        // её удалиться, при это не получать ERROR с PVS ошибкой :(
        // Но пистолет всё ещё пропадает если его убрать из рук.
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
