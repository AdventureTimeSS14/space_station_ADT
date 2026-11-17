// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.ADT.Projectiles;

namespace Content.Server.ADT.Projectiles;

public sealed partial class ProjectileAoESystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileAoEComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileAoEComponent component, ref ProjectileHitEvent ev)
    {
        var shooter = ev.Shooter;
        if (shooter == null || shooter == ev.Target)
            return;

        var target = ev.Target;
        var ents = _lookup.GetEntitiesInRange<DamageableComponent>(Transform(target).Coordinates, component.DamageRadius)
            .Where(e => e.Owner != shooter && e.Owner != target);

        foreach (var ent in ents)
        {
            _damage.TryChangeDamage(ent.Owner, ev.Damage * component.DamageMultiplier, origin: shooter);
        }
    }
}