using Content.Shared.Hands;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Electrocution;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.DNAGunLocker;

public sealed partial class SharedDNAGunLockerSystem : EntitySystem
{

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DNAGunLockerComponent, HandSelectedEvent>(OnHand);
        SubscribeLocalEvent<DNAGunLockerComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
        SubscribeLocalEvent<DNAGunLockerComponent, GotEmaggedEvent>(OnEmaggedPersonalGun);
    }

    private void OnHand(EntityUid uid, DNAGunLockerComponent component, HandSelectedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        //Log.Debug($"{ToPrettyString(uid)} было завладето {ToPrettyString(args.User)}");
        if (TryComp<GunComponent>(uid, out var compGun))
        {
            MakeWeaponPersonal(uid, compGun, component, args.User);
        }
        Dirty(uid, component);
    }

    private void OnAltVerbs(EntityUid uid, DNAGunLockerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        AlternativeVerb verbPersonalize = new()
        {
            Act = () => AltPrivateWeaponPersonal(uid, component, args.User),
            Text = Loc.GetString("gun-personalize-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
        };
        args.Verbs.Add(verbPersonalize);
    }

    /// <summary>
    /// ADT, modern. This method makes weapon personal, making everyone except user not able to shoot with it.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="user"></param>
    /// <param name="popup"></param>
    private void MakeWeaponPersonal(EntityUid uid, GunComponent gunComp, DNAGunLockerComponent dnaComp,
     EntityUid? user = null, bool popup = false)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (dnaComp.GunOwner != null)
        {
            if (popup)
                _popup.PopupPredicted(Loc.GetString("gun-personalize-error"), uid, user);
            return;
        }
        dnaComp.GunOwner = user;
        _popup.PopupPredicted(Loc.GetString("gun-was-personalized"), uid, user);
        _audioSystem.PlayPredicted(dnaComp.LockSound, uid, user);

        Dirty(uid, dnaComp);
    }

    private void AltPrivateWeaponPersonal(EntityUid uid, DNAGunLockerComponent dnaComp, EntityUid user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        // Если оружие еще не имеет владельца
        if (dnaComp.GunOwner == null)
        {
            // Установить пользователя как владельца
            dnaComp.GunOwner = user;
            _popup.PopupPredicted(Loc.GetString("gun-was-personalized"), uid, user);
            _audioSystem.PlayPredicted(dnaComp.LockSound, uid, user);
            return;
        }
        // Если владелец заходит в эту функцию
        else if (dnaComp.GunOwner == user)
        {
            // Сбросить владельца
            dnaComp.GunOwner = null;
            _popup.PopupPredicted(Loc.GetString("gun-personalize-unlocked"), uid, user);
            _audioSystem.PlayPredicted(dnaComp.LockSound, uid, user);
            return;
        }
        // Если заходит другой пользователь, не владелец оружия
        else
        {
            _popup.PopupPredicted(Loc.GetString("gun-personalize-error"), uid, user);
        }
    }

    private void OnEmaggedPersonalGun(EntityUid uid, DNAGunLockerComponent component, GotEmaggedEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if ((ev.Handled || component.IsEmagged) && TryComp<DNAGunLockerComponent>(uid, out var _))
            return;
        _audio.PlayPredicted(component.SparkSound, uid, ev.UserUid);
        _popup.PopupPredicted(Loc.GetString("gun-component-upgrade-emag"), uid, ev.UserUid);
        component.IsEmagged = true;
        ev.Handled = true;
        Dirty(uid, component);
    }
}
