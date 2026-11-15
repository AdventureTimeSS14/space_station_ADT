// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Shared.Body.Systems;

namespace Content.Server.Weapons.Marker;

public sealed partial class LeechMeleeWeaponSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private BloodstreamSystem _blood = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LeechMeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, LeechMeleeWeaponComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        DamageSpecifier? heal = component.Heal?.Clone();
        DamageSpecifier? groupsHeal = null;

        if (component.HealGroups != null && !component.HealGroups.Empty)
        {
            if (component.Weighted)
            {
                groupsHeal = _damageable.CreateWeightedHealFromGroups(args.User, component.HealGroups);
            }
            else
            {
                groupsHeal = _damageable.CreateHealFromGroups(args.User, component.HealGroups);
            }
        }

        if (heal == null && groupsHeal != null)
            heal = groupsHeal;
        else if (heal != null && groupsHeal != null)
            heal += groupsHeal;

        if (heal == null || heal.Empty)
            return;

        foreach (var hitEnt in args.HitEntities)
        {
            if (!_entityWhitelist.IsWhitelistPass(component.Whitelist, hitEnt))
                continue;

            if (_entityWhitelist.IsWhitelistPass(component.Blacklist, hitEnt))
                continue;

            if (_mobState.IsDead(hitEnt))
                continue;

            _damageable.TryChangeDamage(args.User, heal, true, false, origin: args.Weapon);
            _blood.TryModifyBloodLevel(args.User, component.ModifyBloodLevel);
            _blood.TryModifyBleedAmount(args.User, component.BloodlossModifier);
        }
    }
}
