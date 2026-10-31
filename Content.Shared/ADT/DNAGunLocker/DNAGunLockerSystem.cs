using Content.Shared.CombatMode;
using Content.Shared.Electrocution;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.DNAGunLocker;

public sealed class DNAGunLockerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DNAGunLockerComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<DNAGunLockerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DNAGunLockerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DNAGunLockerComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnHandSelected(Entity<DNAGunLockerComponent> ent, ref HandSelectedEvent args)
    {
        if (ent.Comp.GunOwner != null)
            return;

        SetOwner(ent, args.User, args.User);
    }

    private void OnGetVerbs(Entity<DNAGunLockerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TogglePersonalization(ent, user),
            Text = Loc.GetString("gun-personalize-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
            Priority = -20,
        });
    }

    private void TogglePersonalization(Entity<DNAGunLockerComponent> ent, EntityUid user)
    {
        if (ent.Comp.GunOwner == null)
        {
            SetOwner(ent, user, user);
            return;
        }

        if (ent.Comp.GunOwner != user)
        {
            _popup.PopupClient(Loc.GetString("gun-personalize-error"), ent, user);
            return;
        }

        ent.Comp.GunOwner = null;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("gun-personalize-unlocked"), ent, user);
        _audio.PlayPredicted(ent.Comp.LockSound, ent, user);
    }

    private void SetOwner(Entity<DNAGunLockerComponent> ent, EntityUid owner, EntityUid user)
    {
        ent.Comp.GunOwner = owner;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("gun-was-personalized"), ent, user);
        _audio.PlayPredicted(ent.Comp.LockSound, ent, user);
    }

    private void OnEmagged(Entity<DNAGunLockerComponent> ent, ref GotEmaggedEvent args)
    {
        if (args.Handled || ent.Comp.IsEmagged)
            return;

        ent.Comp.IsEmagged = true;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.SparkSound, ent, args.UserUid);
        _popup.PopupClient(Loc.GetString("gun-component-upgrade-emag"), ent, args.UserUid);
        args.Handled = true;
    }

    private void OnShotAttempted(Entity<DNAGunLockerComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || ent.Comp.IsEmagged || ent.Comp.GunOwner == args.User)
            return;

        args.Cancel();

        _electrocution.TryDoElectrocution(args.User, null, ent.Comp.ShockDamage, ent.Comp.ShockTime, refresh: true, ignoreInsulation: true);
        _popup.PopupClient(Loc.GetString("gun-personalize-fuck"), args.User, args.User);
        _audio.PlayPredicted(ent.Comp.ElectricSound, ent, args.User);

        if (TryComp<CombatModeComponent>(args.User, out var combatMode))
            _combatMode.SetInCombatMode(args.User, false, combatMode);
    }
}
