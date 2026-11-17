// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Linq;
using Content.Server.ADT.Damage.Components;
using Content.Server.Damage.Components;
using Content.Shared.ADT.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Damage.Systems;

public sealed partial class DamageInContainerSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageInContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveDamageInContainerComponent, DamageInContainerComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out _, out var comp, out var containerComp))
        {
            if (_gameTiming.CurTime < comp.NextTickTime)
                continue;

            comp.NextTickTime = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.Interval);

            if (!_container.TryGetContainer(uid, comp.SlotId, out var container, containerComp) || container.ContainedEntities.Count == 0)
            {
                RemComp<ActiveDamageInContainerComponent>(uid);
                continue;
            }

            foreach (var contained in container.ContainedEntities)
            {
                if (!TryComp<DamageableComponent>(contained, out var damage))
                    continue;

                var totalDamage = _damageable.GetTotalDamage((contained, damage));
                if (comp.DamageCap != 0 && totalDamage >= comp.DamageCap)
                    continue;

                if (_whitelistSystem.IsWhitelistFail(comp.Whitelist, contained))
                    continue;

                DamageSpecifier? finalDamage = null;
                if (comp.Damage != null && !comp.Damage.Empty && comp.Damage.DamageDict.Values.Any(x => x < 0))
                {
                    finalDamage = comp.Damage.Clone();
                    if (comp.DamageGroups != null && !comp.DamageGroups.Empty)
                    {
                        var groupsHeal = _damageable.CreateWeightedHealFromGroups(contained, comp.DamageGroups);
                        finalDamage += groupsHeal;
                    }
                }
                else if (comp.Damage != null && !comp.Damage.Empty)
                {
                    finalDamage = comp.Damage;
                }
                else if (comp.DamageGroups != null && !comp.DamageGroups.Empty)
                {
                    finalDamage = _damageable.CreateWeightedHealFromGroups(contained, comp.DamageGroups);
                }
                else
                {
                    continue;
                }

                if (finalDamage == null)
                    continue;

                if (TryComp<MobStateComponent>(contained, out var mobState))
                {
                    foreach (var allowedState in comp.AllowedStates)
                    {
                        if (allowedState == mobState.CurrentState)
                        {
                            _damageable.TryChangeDamage(contained, finalDamage, true, false);
                            break;
                        }
                    }
                    continue;
                }

                _damageable.TryChangeDamage(contained, finalDamage, true, false);
            }
        }
    }

    private void OnInserted(EntityUid uid, DamageInContainerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!comp.Initialized)
            return;

        if (args.Container.ID != comp.SlotId)
            return;

		if (!HasComp<ActiveDamageInContainerComponent>(uid))
			AddComp<ActiveDamageInContainerComponent>(uid);
    }
}
