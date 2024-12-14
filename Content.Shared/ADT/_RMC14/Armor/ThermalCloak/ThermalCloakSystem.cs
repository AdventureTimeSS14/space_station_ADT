using Content.Shared._RMC14.Stealth;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Ninja.Systems;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._RMC14.Armor.ThermalCloak;

/// <summary>
/// Handles Thermal Cloak's cloaking ability
/// </summary>
public sealed class ThermalCloakSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalCloakComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ThermalCloakComponent, ThermalCloakTurnInvisibleActionEvent>(OnCloakAction);
        SubscribeLocalEvent<ThermalCloakComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ThermalCloakComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<EntityActiveInvisibleComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<GunComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<ExplodeOnTriggerComponent, UseInHandEvent>(OnTimerUse);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectile);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnGetItemActions(Entity<ThermalCloakComponent> ent, ref GetItemActionsEvent args)
    {
        var comp = ent.Comp;

        if (comp.HandsBlock && _hands.IsHolding(comp.Owner, ent.Owner))
            return;

        if (comp.NinjaSuit && _ninja.IsNinja(ent))
            args.AddAction(ref comp.Action, comp.ActionId);
        else if (!comp.NinjaSuit)
            args.AddAction(ref comp.Action, comp.ActionId);

        Dirty(ent);
    }

    private void OnCloakAction(Entity<ThermalCloakComponent> ent, ref ThermalCloakTurnInvisibleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        SetInvisibility(ent, args.Performer, !ent.Comp.Enabled, false);
    }

    private void OnEquipped(Entity<ThermalCloakComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // if (!_inventory.InSlotWithFlags((ent, null, null), SlotFlags.All))
        //     return;

        var comp = EnsureComp<EntityTurnInvisibleComponent>(args.Equipee);
        comp.RestrictWeapons = ent.Comp.RestrictWeapons;
        comp.UncloakWeaponLock = ent.Comp.UncloakWeaponLock;
        Dirty(args.Equipee, comp);
    }

    private void OnUnequipped(Entity<ThermalCloakComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // if (_inventory.InSlotWithFlags((ent, null, null), SlotFlags.All))
        //     return;

        SetInvisibility(ent, args.Equipee, false, false);
        RemCompDeferred<EntityTurnInvisibleComponent>(args.Equipee);
    }

    public void SetInvisibility(Entity<ThermalCloakComponent> ent, EntityUid user, bool enabling, bool forced)
    {
        if (!TryComp<EntityTurnInvisibleComponent>(user, out var turnInvisible))
            return;

        if (enabling && !HasComp<EntityActiveInvisibleComponent>(user))
        {
            var activeInvisibility = EnsureComp<EntityActiveInvisibleComponent>(user);
            activeInvisibility.Opacity = ent.Comp.Opacity;
            Dirty(user, activeInvisibility);

            ent.Comp.Enabled = true;
            turnInvisible.Enabled = true;
            if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
            {
                action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
                action.UseDelay = ent.Comp.Cooldown;
                Dirty(ent.Comp.Action.Value, action);
            }

            turnInvisible.UncloakTime = _timing.CurTime; // Just in case

            ToggleLayers(user, ent.Comp.CloakedHideLayers, false);
            SpawnCloakEffects(user, ent.Comp.CloakEffect);

            var popupOthers = Loc.GetString("rmc-cloak-activate-others", ("user", user));
            _popup.PopupPredicted(Loc.GetString("rmc-cloak-activate-self"), popupOthers, user, user, PopupType.Medium);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.CloakSound, user);

            return;
        }

        if (!enabling && TryComp<EntityActiveInvisibleComponent>(user, out var invisible))
        {
            invisible.Opacity = 1;
            Dirty(user, invisible);
            ent.Comp.Enabled = false;
            turnInvisible.Enabled = false;

            if (forced)
            {
                if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
                {
                    action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.ForcedCooldown);
                    action.UseDelay = ent.Comp.ForcedCooldown;
                    Dirty(ent.Comp.Action.Value, action);
                }

                turnInvisible.UncloakTime = _timing.CurTime;

                var forcedPopupOthers = Loc.GetString("rmc-cloak-forced-deactivate-others", ("user", user));
                _popup.PopupPredicted(Loc.GetString("rmc-cloak-forced-deactivate-self"), forcedPopupOthers, user, user, PopupType.Medium);
            }
            else
            {
                if (TryComp<InstantActionComponent>(ent.Comp.Action, out var action))
                {
                    action.Cooldown = (_timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
                    action.UseDelay = ent.Comp.Cooldown;
                    Dirty(ent.Comp.Action.Value, action);
                }

                turnInvisible.UncloakTime = _timing.CurTime;
                var popupOthers = Loc.GetString("rmc-cloak-deactivate-others", ("user", user));
                _popup.PopupPredicted(Loc.GetString("rmc-cloak-deactivate-self"), popupOthers, user, user, PopupType.Medium);
            }

            ToggleLayers(user, ent.Comp.CloakedHideLayers, true);
            SpawnCloakEffects(user, ent.Comp.UncloakEffect);

            RemCompDeferred<EntityActiveInvisibleComponent>(user);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.UncloakSound, user);
        }
    }

    public void TrySetInvisibility(EntityUid uid, bool enabling, bool forced, ThermalCloakComponent? component = null)
    {
        var cloak = FindWornCloak(uid);
        if (cloak.HasValue)
            SetInvisibility(cloak.Value, uid, enabling, forced);
    }

    private void OnAttemptShoot(Entity<GunComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !TryComp<EntityTurnInvisibleComponent>(args.User, out var comp))
            return;

        if (comp.RestrictWeapons && comp.Enabled || comp.UncloakTime + comp.UncloakWeaponLock > _timing.CurTime)
        {
            args.Cancelled = true;

            var popup = Loc.GetString("rmc-cloak-attempt-shoot");
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
        }
    }

    private void OnTimerUse(Entity<ExplodeOnTriggerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<EntityTurnInvisibleComponent>(args.User, out var comp))
            return;

        if (comp.RestrictWeapons && comp.Enabled || comp.UncloakTime + comp.UncloakWeaponLock > _timing.CurTime)
        {
            args.Handled = true;

            var popup = Loc.GetString("rmc-cloak-attempt-prime");
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
        }
    }

    private void OnProjectile(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        TrySetInvisibility(args.Target, false, true);
    }

    private void OnMeleeHit(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        TrySetInvisibility(args.User, false, true);
    }

    private void OnMobStateChanged(Entity<EntityActiveInvisibleComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        TrySetInvisibility(ent.Owner, false, true);
    }
    private Entity<ThermalCloakComponent>? FindWornCloak(EntityUid player)
    {
        var slots = _inventory.GetSlotEnumerator(player, SlotFlags.All);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp<ThermalCloakComponent>(slot.ContainedEntity, out var comp))
                return (slot.ContainedEntity.Value, comp);
        }

        return null;
    }

    private void ToggleLayers(EntityUid equipee, HashSet<HumanoidVisualLayers> layers, bool showLayers)
    {
        foreach (HumanoidVisualLayers layer in layers)
        {
            _humanoidSystem.SetLayerVisibility(equipee, layer, showLayers);
        }
    }

    public void SpawnCloakEffects(EntityUid user, EntProtoId cloakProtoId)
    {
        if (_net.IsClient)
            return;

        var coordinates = _transform.GetMapCoordinates(user);
        var rotation = _transform.GetWorldRotation(user);

        Spawn(cloakProtoId, coordinates, rotation: rotation);
    }
}
