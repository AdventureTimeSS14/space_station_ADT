// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    private void InitializeUmbrae()
    {
        SubscribeLocalEvent<VampireComponent, VampireCloakOfDarknessActionEvent>(OnCloakOfDarkness);
        SubscribeLocalEvent<VampireComponent, VampireShadowSnareActionEvent>(OnShadowSnare);
        SubscribeLocalEvent<VampireComponent, VampireSoulAnchorActionEvent>(OnAfterSoulAnchor);
        SubscribeLocalEvent<VampireComponent, SoulAnchorDoAfterEvent>(OnSoulAnchorDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireDarkPassageActionEvent>(OnVampireDarkPassage);
        SubscribeLocalEvent<VampireComponent, VampireExtinguishActionEvent>(OnExtinguish);
        SubscribeLocalEvent<VampireComponent, VampireShadowBoxingActionEvent>(OnShadowBoxing);
        SubscribeLocalEvent<VampireComponent, VampireEternalDarknessActionEvent>(OnEternalDarkness);
        SubscribeLocalEvent<VampireComponent, DamageModifyEvent>(OnCloakDamageModify);
    }

    private const float CloakSpeedMod = 1.3f;
    private const float CloakLightCheckRange = 4f;

    private void OnCloakOfDarkness(Entity<VampireComponent> ent, ref VampireCloakOfDarknessActionEvent args)
    {
        if (ent.Comp.CloakActive)
        {
            DisableCloakOfDarkness(ent);
            _popup.PopupEntity(Loc.GetString("vampire-stealth-disabled"), ent, ent, PopupType.Small);
            args.Handled = true;
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (!TryComp(ent, out MovementSpeedModifierComponent? speedmodComponent))
            return;

        ent.Comp.CloakWalkSpeed = speedmodComponent.BaseWalkSpeed;
        ent.Comp.CloakSprintSpeed = speedmodComponent.BaseSprintSpeed;
        ent.Comp.CloakActive = true;
        ent.Comp.CloakInDarkness = false;
        ent.Comp.NextCloakCheck = 1f;

        var stealth = EnsureComp<StealthComponent>(ent);
        _stealth.SetEnabled(ent, true, stealth);

        UpdateCloakOfDarkness(ent, ent.Comp);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        _popup.PopupEntity(Loc.GetString("vampire-stealth-enabled"), ent, ent, PopupType.Small);
        args.Handled = true;
    }

    private void UpdateCloakOfDarkness(EntityUid uid, VampireComponent component)
    {
        if (!component.CloakActive)
            return;

        if (TryComp<FlammableComponent>(uid, out var flammable) && flammable.OnFire)
        {
            DisableCloakOfDarkness((uid, component));
            _popup.PopupEntity(Loc.GetString("vampire-cloak-broken-fire"), uid, uid, PopupType.MediumCaution);
            return;
        }

        var inDarkness = component.Variant != VampireVariant.Classic || !IsCloakLit(uid);
        if (inDarkness == component.CloakInDarkness)
            return;

        component.CloakInDarkness = inDarkness;

        if (TryComp<StealthComponent>(uid, out var stealth))
            _stealth.SetVisibility(uid, inDarkness ? 0.3f : 0.8f, stealth);

        if (TryComp<MovementSpeedModifierComponent>(uid, out var speed))
        {
            var mod = inDarkness ? CloakSpeedMod : 1f;
            _speed.ChangeBaseSpeed(uid, component.CloakWalkSpeed * mod, component.CloakSprintSpeed * mod, speed.Acceleration);
        }

        Dirty(uid, component);
    }

    private bool IsCloakLit(EntityUid uid)
    {
        if (IsInSpace(uid))
            return true;

        var coords = Transform(uid).Coordinates;
        foreach (var light in _entityLookup.GetEntitiesInRange<PointLightComponent>(coords, CloakLightCheckRange))
        {
            if (light.Owner != uid && light.Comp.Enabled && light.Comp.Energy > 0f)
                return true;
        }

        return false;
    }

    private void DisableCloakOfDarkness(Entity<VampireComponent> vampire)
    {
        if (!vampire.Comp.CloakActive)
            return;

        vampire.Comp.CloakActive = false;
        vampire.Comp.CloakInDarkness = false;

        if (TryComp<StealthComponent>(vampire, out var stealth))
            _stealth.SetEnabled(vampire, false, stealth);

        if (TryComp<MovementSpeedModifierComponent>(vampire, out var speed))
            _speed.ChangeBaseSpeed(vampire, vampire.Comp.CloakWalkSpeed, vampire.Comp.CloakSprintSpeed, speed.Acceleration);

        Dirty(vampire.Owner, vampire.Comp);
    }

    private void OnCloakDamageModify(Entity<VampireComponent> ent, ref DamageModifyEvent args)
    {
        if (!ent.Comp.CloakActive)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, new DamageModifierSet
        {
            Coefficients = new Dictionary<ProtoId<DamageTypePrototype>, float>
            {
                { "Heat", 1.3f },
                { "Shock", 1.3f }
            }
        });
    }

    private void OnShadowSnare(Entity<VampireComponent> ent, ref VampireShadowSnareActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var targetCoords = args.Target;
        if (TrySpawnObjectAtPosition(targetCoords, args.EntityId, ent))
        {
            SubtractBloodEssence(ent.Owner, args.BloodCost);
            args.Handled = true;
        }
    }

    private void OnAfterSoulAnchor(Entity<VampireComponent> ent, ref VampireSoulAnchorActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        EntityUid? beaconEntity = null;
        var beaconQuery = EntityQueryEnumerator<BeaconSoulComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out var beaconComp))
        {
            if (beaconComp.VampireOwner == ent.Owner)
            {
                beaconEntity = beaconUid;
                break;
            }
        }

        if (beaconEntity.HasValue)
        {
            RaiseLocalEvent(ent, new SoulAnchorDoAfterEvent(args.BloodCost));
            args.Handled = true;
            return;
        }

        args.Handled = true;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(15f), new SoulAnchorDoAfterEvent(args.BloodCost), ent)
        {
            BreakOnMove = false,
            NeedHand = false,
        });
    }

    private void OnSoulAnchorDoAfter(Entity<VampireComponent> ent, ref SoulAnchorDoAfterEvent args)
    {
        EntityUid? beaconEntity = null;
        var beaconQuery = EntityQueryEnumerator<BeaconSoulComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out var beaconComp))
        {
            if (beaconComp.VampireOwner == ent.Owner)
            {
                beaconEntity = beaconUid;
                break;
            }
        }

        if (beaconEntity.HasValue)
        {
            _transform.SetCoordinates(ent, Transform(beaconEntity.Value).Coordinates);
            Del(beaconEntity.Value);

            SubtractBloodEssence(ent.Owner, args.BloodCost);
        }
        else
        {
            var beaconEntityNew = Spawn(args.EntityId, Transform(ent).Coordinates);
            var beaconComponent = EnsureComp<BeaconSoulComponent>(beaconEntityNew);
            beaconComponent.VampireOwner = ent;
        }
    }

    private void OnVampireDarkPassage(Entity<VampireComponent> ent, ref VampireDarkPassageActionEvent args)
    {
        var targetCoords = args.Target;
        if (!HasTruePower(ent))
        {
            if (!_interaction.InRangeUnobstructed(ent, targetCoords, range: 1000F, collisionMask: CollisionGroup.Impassable, popup: false))
            {
                _popup.PopupEntity(Loc.GetString("vampire-teleport-failed"), ent, ent, PopupType.Small);
                return;
            }
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (HasComp<NullRodOwnerComponent>(targetCoords.EntityId) && !HasTruePower(ent))
            return;

        var currentCoords = Transform(ent).Coordinates;
        _transform.SetCoordinates(ent, targetCoords);

        Spawn(args.MistEffect, currentCoords);
        Spawn(args.MistReappearEffect, targetCoords);

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnExtinguish(Entity<VampireComponent> ent, ref VampireExtinguishActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        DamageLightsInRange(ent, 15f, args.Damage);

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnShadowBoxing(Entity<VampireComponent> ent, ref VampireShadowBoxingActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        ExecuteShadowBoxingTick(ent, args, 0);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnEternalDarkness(Entity<VampireComponent> ent, ref VampireEternalDarknessActionEvent args)
    {
        var supreme = GetTruePower(ent);
        if (supreme == null)
            return;

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var netEntity = GetNetEntity(ent);

        if (supreme.Active)
        {
            supreme.Active = false;
            _alerts.ClearAlert(ent.Owner, args.Alert);
            Dirty(ent, supreme);

            args.Handled = true;
            return;
        }

        _alerts.ShowAlert(ent.Owner, args.Alert, 0);
        supreme.Active = true;
        Dirty(ent, supreme);

        _popup.PopupEntity(Loc.GetString("vampire-blood-true-power-started"), ent, ent, PopupType.SmallCaution);

        ExecuteEternalDarknessTick(ent, supreme, args, netEntity);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    #region Utility Methods

    private void ExecuteShadowBoxingTick(EntityUid uid, VampireShadowBoxingActionEvent args, int currentTick)
    {
        if (!Exists(uid) || currentTick >= args.Repeats)
            return;

        Spawn(args.EntityId, Transform(uid).Coordinates);

        Timer.Spawn(args.TimeInterval, () => ExecuteShadowBoxingTick(uid, args, currentTick + 1));
    }

    private void ExecuteEternalDarknessTick(Entity<VampireComponent> ent, SupremeVampireComponent supreme, VampireEternalDarknessActionEvent args, NetEntity netEntity)
    {
        if (!Exists(ent) || !supreme.Active)
        {
            _alerts.ClearAlert(ent.Owner, args.Alert);
            supreme.Active = false;
            Dirty(ent, supreme);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);

            supreme.Active = false;
            _alerts.ClearAlert(ent.Owner, args.Alert);
            Dirty(ent, supreme);
            return;
        }

        CoolSurroundingAtmosphere(ent);
        DamageLightsInRange(ent, 4f, args.Damage);
        SubtractBloodEssence(ent.Owner, args.BloodCost);

        Timer.Spawn(args.TimeInterval, () => ExecuteEternalDarknessTick(ent, supreme, args, netEntity));
    }

    private void DamageLightsInRange(Entity<VampireComponent> ent, float radius, DamageSpecifier damage)
    {
        var coords = Transform(ent).Coordinates;
        var lightsInRange = _entityLookup.GetEntitiesInRange<PointLightComponent>(coords, radius)
            .Where(entity => HasComp<DamageableComponent>(entity.Owner)
                && !HasComp<MobStateComponent>(entity.Owner)).ToList();

        foreach (var lightEntity in lightsInRange)
        {
            _damage.TryChangeDamage(lightEntity.Owner, damage, true, origin: ent);
        }
    }

    private void CoolSurroundingAtmosphere(EntityUid ent)
    {
        if (_atmosphere.GetContainingMixture(ent, excite: true) is { } atmosphere)
        {
            const float targetTemperature = 233.15f;
            const float coolingRate = 20000f;

            var deltaT = targetTemperature - atmosphere.Temperature;
            if (deltaT < 0)
            {
                var heatCapacity = _atmosphere.GetHeatCapacity(atmosphere, true);
                var energyToRemove = Math.Min(Math.Abs(deltaT) * heatCapacity, coolingRate);

                _atmosphere.AddHeat(atmosphere, -energyToRemove);
            }
        }
    }

    #endregion
}
