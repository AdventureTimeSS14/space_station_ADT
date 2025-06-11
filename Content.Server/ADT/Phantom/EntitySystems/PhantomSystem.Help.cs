using Content.Server.Power.Components;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Damage;
using Content.Shared.Eye;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomSystem
{
    private void InitializeHelpAbilities()
    {
        SubscribeLocalEvent<PhantomComponent, RepairActionEvent>(OnRepair);
        SubscribeLocalEvent<PhantomComponent, GhostHealActionEvent>(OnGhostHeal);
        SubscribeLocalEvent<PhantomComponent, GhostClawsActionEvent>(OnGhostClaws);
        SubscribeLocalEvent<PhantomComponent, PhantomPortalActionEvent>(OnPortal);
        SubscribeLocalEvent<PhantomComponent, PhantomHelpingHelpActionEvent>(OnHelpingHand);

    }

    private void OnRepair(EntityUid uid, PhantomComponent component, RepairActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryUseAbility(uid, args, target))
            return;

        bool success = false;
        if (TryComp<BatteryComponent>(target, out var batteryComp))
        {
            _batterySystem.SetCharge(target, batteryComp.MaxCharge);
            success = true;
        }

        if (HasComp<BatteryComponent>(target) || HasComp<PowerCellSlotComponent>(target))
        {
            var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
            var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
            _damageableSystem.TryChangeDamage(target, damage_brute);
            _damageableSystem.TryChangeDamage(target, damage_burn);
            success = true;
        }

        if (TryComp<ContainerManagerComponent>(target, out var containerManagerComponent))
        {
            foreach (var container in containerManagerComponent.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<BatteryComponent>(entity, out var entBatteryComp))
                    {
                        _batterySystem.SetCharge(entity, entBatteryComp.MaxCharge);
                        success = true;
                    }

                    if (HasComp<BatteryComponent>(target) || HasComp<PowerCellSlotComponent>(target))
                    {
                        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
                        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
                        _damageableSystem.TryChangeDamage(target, damage_brute);
                        _damageableSystem.TryChangeDamage(target, damage_burn);
                        success = true;
                    }
                }
            }
        }

        if (success)
        {
            if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
                _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/repair.ogg"), mind.Session);
            var selfMessage = Loc.GetString("phantom-repair-self", ("name", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
        }
    }

    private void OnGhostHeal(EntityUid uid, PhantomComponent component, GhostHealActionEvent args)
    {
        if (args.Handled)
            return;

        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        var target = component.Holder;

        if (!TryUseAbility(uid, args, target))
            return;

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (_playerManager.TryGetSessionByEntity(target, out var targetSession))
            _audio.PlayGlobal(args.Sound, targetSession);


        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(target, damage_brute);
        _damageableSystem.TryChangeDamage(target, damage_burn);
    }

    private void OnGhostClaws(EntityUid uid, PhantomComponent component, GhostClawsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        var target = component.Holder;

        if (!TryUseAbility(uid, args, target))
            return;

        UpdateEctoplasmSpawn(uid);
        args.Handled = true;

        if (!TryComp(target, out InventoryComponent? inventory))
            return;

        if (!component.ClawsOn)
        {
            var claws = Spawn("ADTGhostClaws", Transform(target).Coordinates);
            EnsureComp<UnremoveableComponent>(claws);

            _inventorySystem.TryUnequip(target, "gloves", true, true, false, inventory);
            _inventorySystem.TryEquip(target, claws, "gloves", true, true, false, inventory);
            component.Claws = claws;
            var message = Loc.GetString("phantom-claws-appear", ("name", Identity.Entity(target, EntityManager)));
            var selfMessage = Loc.GetString("phantom-claws-appear-self");
            _popup.PopupEntity(message, target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
            _popup.PopupEntity(selfMessage, target, target, PopupType.MediumCaution);
        }
        else
        {
            QueueDel(component.Claws);
            var message = Loc.GetString("phantom-claws-disappear", ("name", Identity.Entity(target, EntityManager)));
            var selfMessage = Loc.GetString("phantom-claws-disappear-self");
            _popup.PopupEntity(message, target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
            _popup.PopupEntity(selfMessage, target, target, PopupType.MediumCaution);

            component.Claws = new();
        }
        component.ClawsOn = !component.ClawsOn;
    }

    private void OnPortal(EntityUid uid, PhantomComponent component, PhantomPortalActionEvent args)
    {
        if (args.Handled)
            return;

        var coordinates = Transform(uid).Coordinates;

        // No portals exist
        if (Deleted(component.Portal1) && Deleted(component.Portal2))
        {
            var portal = Spawn(component.PortalPrototype, coordinates);
            EnsureComp<PhantomPortalComponent>(portal);
            _visibility.SetLayer((portal, EnsureComp<VisibilityComponent>(portal)), (int)VisibilityFlags.PhantomVessel);
            _visibility.RefreshVisibility(portal);
            component.Portal1 = portal;
            _audio.PlayPvs(component.PortalSound, portal);
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
            return;
        }

        // One portal exists
        if (!Deleted(component.Portal1) && Deleted(component.Portal2))
        {
            if (Transform(component.Portal1).Coordinates.TryDistance(EntityManager, coordinates, out var distance) &&
                distance > 10f)
            {
                var message = Loc.GetString("phantom-portal-too-far");
                _popup.PopupEntity(message, uid, uid);
                return;
            }

            var portal = Spawn(component.PortalPrototype, coordinates);
            _visibility.SetLayer((portal, EnsureComp<VisibilityComponent>(portal)), (int)VisibilityFlags.PhantomVessel);
            _visibility.RefreshVisibility(portal);
            var firstPortalComp = EnsureComp<PhantomPortalComponent>(component.Portal1);
            var secondPortalComp = EnsureComp<PhantomPortalComponent>(portal);
            component.Portal2 = portal;
            firstPortalComp.LinkedPortal = portal;
            secondPortalComp.LinkedPortal = component.Portal1;
            _audio.PlayPvs(component.PortalSound, portal);
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
            return;
        }

        // Both portals exist
        if (!Deleted(component.Portal1) && !Deleted(component.Portal2))
        {
            QueueDel(component.Portal1);
            QueueDel(component.Portal2);
            component.Portal1 = new();
            component.Portal2 = new();
            UpdateEctoplasmSpawn(uid);
            args.Handled = true;
            return;
        }
    }

    private void OnHelpingHand(EntityUid uid, PhantomComponent component, PhantomHelpingHelpActionEvent args)
    {
        if (args.Handled)
            return;

        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-no-holder");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        var target = component.Holder;

        if (!TryUseAbility(uid, args, target))
            return;

        if (!_playerManager.TryGetSessionByEntity(target, out var session))
        {
            var failMessage = Loc.GetString("phantom-no-mind");
            _popup.PopupEntity(failMessage, uid, uid);
            return;
        }

        args.Handled = true;

        UpdateEctoplasmSpawn(uid);
        _euiManager.OpenEui(new AcceptHelpingHandEui(target, this, component), session);
    }

    /// <summary>
    /// Inserts target into helping hand container
    /// </summary>
    /// <param name="target">Target uid</param>
    /// <param name="component">Phantom component</param>
    public void OnHelpingHandAccept(EntityUid target, PhantomComponent component)
    {
        if (!component.HasHaunted)
        {
            var selfMessage = Loc.GetString("phantom-helping-hand-toolate");
            _popup.PopupEntity(selfMessage, target, target);
            return;
        }
        StopHaunt(component.Owner, target, component);
        component.HelpingHandTimer = component.HelpingHandDuration;
        _container.Insert(target, component.HelpingHand, Transform(component.Owner));
        component.TransferringEntity = target;
    }
}
