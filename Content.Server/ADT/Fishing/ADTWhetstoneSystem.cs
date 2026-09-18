using Content.Shared.ADT.Fishing.Components;
using Content.Shared.Interaction;
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
    }

    private void OnAfterInteract(Entity<ADTWhetstoneComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, target) || !TryComp<MeleeWeaponComponent>(target, out var melee))
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-wrong-item", ("item", target)), ent.Owner, args.User);
            return;
        }

        args.Handled = true;

        if (melee.Damage.GetTotal() >= ent.Comp.MaxDamage)
        {
            _popup.PopupEntity(Loc.GetString("adt-whetstone-too-sharp", ("item", target)), ent.Owner, args.User);
            return;
        }

        melee.Damage += ent.Comp.Increment;
        Dirty(target, melee);

        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("adt-whetstone-sharpened", ("item", target)), ent.Owner, args.User);

        ent.Comp.Uses--;

        if (ent.Comp.Uses <= 0)
            QueueDel(ent.Owner);
    }
}
