// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.ADT.Damage.Components;
using Content.Shared.ADT.Damage.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Server.ADT.Weapons.Systems;

public sealed partial class LeechMeleeWeaponSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private GroupHealSystem _groupHeal = default!;
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

        if ((component.Heal == null || component.Heal.Empty) && (component.HealGroups == null || component.HealGroups.Empty))
            return;

        foreach (var hitEnt in args.HitEntities)
        {
            if (!_entityWhitelist.IsWhitelistPass(component.Whitelist, hitEnt))
                continue;

            if (_entityWhitelist.IsWhitelistPass(component.Blacklist, hitEnt))
                continue;

            if (_mobState.IsDead(hitEnt))
                continue;

            _groupHeal.Heal(args.User, component.Heal, component.HealGroups, weighted: component.Weighted, origin: args.Weapon);
            _blood.TryModifyBloodLevel(args.User, component.ModifyBloodLevel);
            _blood.TryModifyBleedAmount(args.User, component.BloodlossModifier);
        }
    }
}
