using Content.Shared.ADT.Fishing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Fishing;

public sealed class ADTWhetstoneSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTWhetstoneComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTWhetstoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTWhetstoneComponent, ExaminedEvent>(OnWhetstoneExamined);
        SubscribeLocalEvent<ADTSharpenedComponent, ExaminedEvent>(OnSharpenedExamined);
    }

    private void OnAfterInteract(Entity<ADTWhetstoneComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = true;
        TrySharpen(ent, target, args.User);
    }

    private void OnUseInHand(Entity<ADTWhetstoneComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TrySharpen(ent, args.User, args.User, true);
    }

    private void TrySharpen(Entity<ADTWhetstoneComponent> ent, EntityUid target, EntityUid user, bool claws = false)
    {
        if (ent.Comp.Uses <= 0)
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-worn-out"), ent.Owner, user);
            return;
        }

        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-wrong-item", ("item", target)), ent.Owner, user);
            return;
        }

        if (!TryComp<MeleeWeaponComponent>(target, out var melee))
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-wrong-item", ("item", target)), ent.Owner, user);
            return;
        }

        if (ent.Comp.RequiresSharp && !IsSharp(target, melee))
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-not-sharp"), ent.Owner, user);
            return;
        }

        if (HasComp<ADTSharpenedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-already-sharpened", ("item", target)), ent.Owner, user);
            return;
        }

        if (melee.Damage.GetTotal() >= ent.Comp.MaxDamage)
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-too-sharp", ("item", target)), ent.Owner, user);
            return;
        }

        melee.Damage += ent.Comp.Increment;
        Dirty(target, melee);

        if (TryComp<DamageOtherOnHitComponent>(target, out var thrown))
        {
            thrown.Damage += ent.Comp.Increment;
            Dirty(target, thrown);
        }

        EnsureComp<ADTSharpenedComponent>(target);

        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);

        var message = claws ? "adt-whetstone-sharpened-claws" : "adt-whetstone-sharpened";
        _popup.PopupEntity(Loc.GetString(message, ("item", target)), ent.Owner, user);

        ent.Comp.Uses--;
    }

    private bool IsSharp(EntityUid uid, MeleeWeaponComponent melee)
    {
        if (HasComp<SharpComponent>(uid))
            return true;

        return melee.Damage.DamageDict.TryGetValue("Slash", out var slash) && slash > 0
            || melee.Damage.DamageDict.TryGetValue("Piercing", out var pierce) && pierce > 0;
    }

    private void OnWhetstoneExamined(Entity<ADTWhetstoneComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Uses > 0)
            args.PushMarkup(Loc.GetString("adt-whetstone-examine-uses", ("uses", ent.Comp.Uses)));
        else
            args.PushMarkup(Loc.GetString("adt-whetstone-examine-worn-out"));
    }

    private void OnSharpenedExamined(Entity<ADTSharpenedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-whetstone-examine-sharpened"));
    }
}
