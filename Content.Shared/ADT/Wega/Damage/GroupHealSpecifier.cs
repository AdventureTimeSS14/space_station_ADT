// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using System.Linq;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage;

/// <summary>
///     Represents a collection of damage groups and healing values.
///     Designed specifically for healing operations where the healing amount
///     should be distributed only among existing damage types of each group.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class GroupHealSpecifier : IEquatable<GroupHealSpecifier>
{
    /// <summary>
    ///     Dictionary mapping damage group prototypes to total healing amounts for that group.
    ///     Healing amounts should be negative values (e.g., -8 for 8 points of healing).
    /// </summary>
    [DataField("groups")]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GroupHealDict { get; set; } = new();

    /// <summary>
    ///     Returns true if the specifier contains no healing entries.
    /// </summary>
    public bool Empty => GroupHealDict.Count == 0;

    /// <summary>
    ///     Returns the total healing amount across all groups.
    /// </summary>
    public FixedPoint2 GetTotalHealing()
    {
        var total = FixedPoint2.Zero;
        foreach (var value in GroupHealDict.Values)
        {
            total += value;
        }
        return total;
    }

    public GroupHealSpecifier()
    {
    }

    public GroupHealSpecifier(GroupHealSpecifier other)
    {
        GroupHealDict = new Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2>(other.GroupHealDict);
    }

    public GroupHealSpecifier(ProtoId<DamageGroupPrototype> group, FixedPoint2 healingAmount)
    {
        GroupHealDict = new() { { group, healingAmount } };
    }

    public GroupHealSpecifier Clone()
    {
        return new GroupHealSpecifier(this);
    }

    public override string ToString()
    {
        return "GroupHealSpecifier(" + string.Join("; ", GroupHealDict.Select(x => x.Key + ":" + x.Value)) + ")";
    }

    #region Operators

    public static GroupHealSpecifier operator *(GroupHealSpecifier spec, FixedPoint2 factor)
    {
        var result = new GroupHealSpecifier();
        foreach (var (group, amount) in spec.GroupHealDict)
        {
            result.GroupHealDict.Add(group, amount * factor);
        }
        return result;
    }

    public static GroupHealSpecifier operator *(GroupHealSpecifier spec, float factor)
    {
        var result = new GroupHealSpecifier();
        foreach (var (group, amount) in spec.GroupHealDict)
        {
            result.GroupHealDict.Add(group, amount * factor);
        }
        return result;
    }

    public static GroupHealSpecifier operator *(FixedPoint2 factor, GroupHealSpecifier spec) => spec * factor;
    public static GroupHealSpecifier operator *(float factor, GroupHealSpecifier spec) => spec * factor;

    public static GroupHealSpecifier operator +(GroupHealSpecifier a, GroupHealSpecifier b)
    {
        var result = new GroupHealSpecifier(a);
        foreach (var (group, amount) in b.GroupHealDict)
        {
            if (!result.GroupHealDict.TryAdd(group, amount))
            {
                result.GroupHealDict[group] += amount;
            }
        }
        return result;
    }

    #endregion

    public bool Equals(GroupHealSpecifier? other)
    {
        if (other == null || GroupHealDict.Count != other.GroupHealDict.Count)
            return false;

        foreach (var (key, value) in GroupHealDict)
        {
            if (!other.GroupHealDict.TryGetValue(key, out var otherValue) || value != otherValue)
                return false;
        }

        return true;
    }

    public FixedPoint2 this[string key] => GroupHealDict[key];
}
