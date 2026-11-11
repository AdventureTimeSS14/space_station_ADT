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
    [Dependency] private readonly HereticMoonAbilitySystem _moonAbility = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonAmuletComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MoonAmuletComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MoonAmuletComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<MoonAmuletComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MoonAmuletComponent, UserActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnEquipped(Entity<MoonAmuletComponent> ent, ref GotEquippedEvent args)
    {
        var wearer = args.EquipTarget;

        if (!HasComp<MobStateComponent>(wearer))
            return;

        if (IsHereticOrLunatic(wearer))
            return;

        AdjustSanity(wearer, -50f);
        _popup.PopupEntity(Loc.GetString("heretic-amulet-victim"), wearer, wearer, PopupType.Medium);
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

        if (TryChannelAmulet(args.User, args.Target))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<MoonAmuletComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (TryChannelAmulet(args.User, args.Target.Value))
            args.Handled = true;
    }

    private void OnActivateInWorld(Entity<MoonAmuletComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (TryChannelAmulet(args.User, args.Target))
            args.Handled = true;
    }

    public void OnMobInteractUsing(Entity<MobStateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<MoonAmuletComponent>(args.Used))
            return;

        if (TryChannelAmulet(args.User, ent.Owner))
            args.Handled = true;
    }

    private bool TryChannelAmulet(EntityUid user, EntityUid target)
    {
        if (!IsHereticOrLunatic(user) || target == user || !HasComp<MobStateComponent>(target))
            return false;

        _moonAbility.ChannelAmulet(user, target);
        return true;
    }
}