// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Weapons.Multishot;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.Weapons.Multishot;

public sealed class SharedMultishotSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedStaminaSystem _staminaSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultishotComponent, GotEquippedHandEvent>(OnEquipWeapon);
        SubscribeLocalEvent<MultishotComponent, GotUnequippedHandEvent>(OnUnequipWeapon);
        SubscribeLocalEvent<MultishotComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<MultishotComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<MultishotComponent, ExaminedEvent>(OnExamined);
        SubscribeAllEvent<RequestShootEvent>(OnRequestShoot);
    }

    private void OnRequestShoot(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null ||
            !_combatSystem.IsInCombatMode(user))
            return;

        var gunsEnumerator = GetMultishotGuns(user.Value);
        var shootCoords = GetCoordinates(msg.Coordinates);
        var target = GetEntity(msg.Target);

        foreach(var gun in gunsEnumerator)
        {
            var (gunEnt, gunComp, _) = gun;

            if (!HasComp<MultishotComponent>(GetEntity(msg.Gun)) && gunEnt != GetEntity(msg.Gun))
                continue;

            _gunSystem.AttemptShoot(user.Value, (gunEnt, gunComp), shootCoords, target);
        }
    }

    private void OnGunShot(EntityUid uid, MultishotComponent comp, ref GunShotEvent args)
    {
        if (!comp.MultishotAffected)
            return;

        DealStaminaDamage(uid, comp, args.User);
    }


    private void DealStaminaDamage(EntityUid weapon, MultishotComponent component, EntityUid target)
    {
        if (component.StaminaDamage == 0)
            return;

        _staminaSystem.TakeStaminaDamage(target, component.StaminaDamage, source: target, with: weapon, visual: false);
    }

    private void OnRefreshModifiers(EntityUid uid, MultishotComponent comp, ref GunRefreshModifiersEvent args)
    {
        if (!comp.MultishotAffected)
            return;

        args.MaxAngle = args.MaxAngle * comp.SpreadMultiplier + Angle.FromDegrees(comp.SpreadAddition);
        args.MinAngle = args.MinAngle * comp.SpreadMultiplier + Angle.FromDegrees(comp.SpreadAddition);
    }

    private void OnEquipWeapon(Entity<MultishotComponent> multishotWeapon, ref GotEquippedHandEvent args)
    {
        var gunsEnumerator = GetMultishotGuns(args.User);

        if (gunsEnumerator.Count < 2)
            return;

        foreach (var gun in gunsEnumerator)
        {
            gun.Item3.MultishotAffected = true;
            Dirty(gun.Item1, gun.Item3);
            _gunSystem.RefreshModifiers(gun.Item1);
        }
    }

    private void OnUnequipWeapon(Entity<MultishotComponent> multishotWeapon, ref GotUnequippedHandEvent args)
    {
        var gunsEnumerator = GetMultishotGuns(args.User);

        multishotWeapon.Comp.MultishotAffected = false;
        _gunSystem.RefreshModifiers(multishotWeapon.Owner);
        Dirty(multishotWeapon);

        if (gunsEnumerator.Count >= 2)
            return;

        foreach (var gun in gunsEnumerator)
        {
            gun.Item3.MultishotAffected = false;
            Dirty(gun.Item1, gun.Item3);
            _gunSystem.RefreshModifiers(gun.Item1);
        }
    }

    private void OnExamined(EntityUid uid, MultishotComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("[color=#E0C068]Из этого оружия можно стрелять из двух рук.[/color]")); // ADT-Goobstation - Multishot - Russian examined text
    }

    /// <summary>
    /// Return list of guns in hands
    /// </summary>
    private List<(EntityUid, GunComponent, MultishotComponent)> GetMultishotGuns(EntityUid entity)
    {
        var handsItems = _handsSystem.EnumerateHeld(entity);
        var itemList = new List<(EntityUid, GunComponent, MultishotComponent)>();

        if (!handsItems.Any())
            return itemList;

        foreach (var item in handsItems)
        {
            if (TryComp<GunComponent>(item, out var gunComp) && TryComp<MultishotComponent>(item, out var multishotComp))
                itemList.Add((item, gunComp, multishotComp));
        }

        return itemList;
    }
}
