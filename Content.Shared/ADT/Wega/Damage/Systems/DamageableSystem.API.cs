// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    /// <summary>
    /// Creates a <see cref="DamageSpecifier"/> from a <see cref="GroupHealSpecifier"/>, distributing healing
    /// evenly only among the damage types from each group that are actually present on the target.
    /// </summary>
    /// <param name="target">The target entity whose current damage is being examined</param>
    /// <param name="groupHealSpec">The group healing specification</param>
    /// <param name="damageableQuery">Optional query for accessing <see cref="DamageableComponent"/></param>
    /// <returns><see cref="DamageSpecifier"/> with healing distributed only to existing damage types</returns>
    public DamageSpecifier CreateHealFromGroups(
        EntityUid target,
        GroupHealSpecifier groupHealSpec,
        EntityQuery<DamageableComponent>? damageableQuery = null)
    {
        var result = new DamageSpecifier();
        if (groupHealSpec.Empty)
            return result;

        var query = damageableQuery ?? _damageableQuery;
        if (!query.TryGetComponent(target, out var damageable))
            return result;

        foreach (var (groupId, healAmount) in groupHealSpec.GroupHealDict)
        {
            if (healAmount >= FixedPoint2.Zero)
                continue;

            if (!ProtoMan.TryIndex(groupId, out var groupProto))
                continue;

            var existingTypes = new List<ProtoId<DamageTypePrototype>>();
            foreach (var damageType in groupProto.DamageTypes)
            {
                if (damageable.Damage.DamageDict.TryGetValue(damageType, out var value) && value > 0)
                    existingTypes.Add(damageType);
            }

            if (existingTypes.Count == 0)
                continue;

            var remainingHealing = -healAmount;
            var remainingTypes = existingTypes.Count;

            foreach (var damageType in existingTypes)
            {
                var healForType = remainingHealing / remainingTypes;

                if (result.DamageDict.TryGetValue(damageType, out var currentValue))
                    result.DamageDict[damageType] = currentValue - healForType;
                else
                    result.DamageDict.Add(damageType, -healForType);

                remainingHealing -= healForType;
                remainingTypes--;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a <see cref="DamageSpecifier"/> from a <see cref="GroupHealSpecifier"/>, distributing healing
    /// by weight (proportional to how much damage of each type is present) only among the damage
    /// types from each group that are actually present on the target.
    /// </summary>
    /// <param name="target">The target entity whose current damage is being examined</param>
    /// <param name="groupHealSpec">The group healing specification</param>
    /// <param name="damageableQuery">Optional query for accessing <see cref="DamageableComponent"/></param>
    /// <returns><see cref="DamageSpecifier"/> with healing distributed by weight to existing damage types</returns>
    public DamageSpecifier CreateWeightedHealFromGroups(
        EntityUid target,
        GroupHealSpecifier groupHealSpec,
        EntityQuery<DamageableComponent>? damageableQuery = null)
    {
        var result = new DamageSpecifier();
        if (groupHealSpec.Empty)
            return result;

        var query = damageableQuery ?? _damageableQuery;
        if (!query.TryGetComponent(target, out var damageable))
            return result;

        foreach (var (groupId, healAmount) in groupHealSpec.GroupHealDict)
        {
            if (healAmount >= FixedPoint2.Zero)
                continue;

            if (!ProtoMan.TryIndex(groupId, out var groupProto))
                continue;

            var existingDamage = new List<(ProtoId<DamageTypePrototype> Type, FixedPoint2 Amount)>();
            FixedPoint2 totalDamage = FixedPoint2.Zero;

            foreach (var damageType in groupProto.DamageTypes)
            {
                if (damageable.Damage.DamageDict.TryGetValue(damageType, out var value) && value > 0)
                {
                    existingDamage.Add((damageType, value));
                    totalDamage += value;
                }
            }

            if (existingDamage.Count == 0 || totalDamage == 0)
                continue;

            var totalHealing = -healAmount;
            foreach (var (damageType, currentDamage) in existingDamage)
            {
                var weight = currentDamage / totalDamage;
                var healForType = totalHealing * weight;
                if (healForType > 0)
                {
                    if (result.DamageDict.TryGetValue(damageType, out var currentValue))
                        result.DamageDict[damageType] = currentValue - healForType;
                    else
                        result.DamageDict.Add(damageType, -healForType);
                }
            }
        }

        return result;
    }
}
