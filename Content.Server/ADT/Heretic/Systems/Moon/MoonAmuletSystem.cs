using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.ADT.Heretic.Systems.Moon;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.Heretic.Systems.Moon;

public sealed partial class MoonAmuletSystem : SharedHereticMoonSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonAmuletComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MoonAmuletComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MoonAmuletComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MoonAmuletComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnEquipped(Entity<MoonAmuletComponent> ent, ref GotEquippedEvent args)
    {
        var wearer = args.Equipee;

        if (!HasComp<MobStateComponent>(wearer))
            return;

        if (IsHereticOrLunatic(wearer))
            return;

        AdjustSanity(wearer, -50f);
        _popup.PopupEntity(Loc.GetString("heretic-amulet-victim"), wearer, wearer, PopupType.Medium);
    }

    private void OnUnequipped(Entity<MoonAmuletComponent> ent, ref GotUnequippedEvent args)
    {
    }

    private void OnExamine(Entity<MoonAmuletComponent> ent, ref ExaminedEvent args)
    {
        if (!IsHereticOrLunatic(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("heretic-amulet-examine-heretic"));
    }

    private void OnInteractHand(Entity<MoonAmuletComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        var target = args.Target;

        if (!IsHereticOrLunatic(user))
            return;

        if (target == user)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;

        EntitySystem.Get<HereticMoonAbilitySystem>().ChannelAmulet(user, target);
    }
}
