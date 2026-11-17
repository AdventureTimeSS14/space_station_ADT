// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Server.ADT.Virology;
using Content.Shared.ADT.Projectiles;
using Content.Shared.Projectiles;
using Robust.Shared.Random;

namespace Content.Server.ADT.Projectiles;

public sealed partial class ProjectileInfectSystem : EntitySystem
{
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileInfectComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileInfectComponent component, ref ProjectileHitEvent ev)
    {
        var shooter = ev.Shooter;
        if (shooter == null || shooter == ev.Target)
            return;

        if (!_random.Prob(component.Prob))
            return;

        _disease.Contract(ev.Target, component.Infection);
    }
}