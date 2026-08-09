// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Construction.Events;

public struct MachinePartState
{
    public MachinePartComponent Part;
    public StackComponent? Stack;

    public readonly int Quantity()
    {
        return Stack?.Count ?? 1;
    }
}

/// <summary>
/// Raised on a machine when its parts change. Machines apply part-based upgrades here.
/// </summary>
public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartState> Parts = new List<MachinePartState>();
    public Dictionary<ProtoId<MachinePartPrototype>, float> PartRatings = new();

    public float GetPartRating(ProtoId<MachinePartPrototype> partId, float defaultValue = 1f)
    {
        return PartRatings.GetValueOrDefault(partId, defaultValue);
    }

    public float GetPartRatingSum(ProtoId<MachinePartPrototype> partId)
    {
        var sum = 0f;
        foreach (var state in Parts)
        {
            if (state.Part.Part != partId)
                continue;

            sum += state.Part.Tier * state.Quantity();
        }

        return sum;
    }

    public static float GetTierDiscount(float tier, float step, float min = 0.5f)
    {
        return Math.Clamp(1f - (tier - 1f) * step, min, 1f);
    }

    public static float GetPositiveTierMultiplier(float tier, float @base = 1f)
    {
        return Math.Max(@base, tier);
    }
}

/// <summary>
/// Raised on a machine when the "Upgrades" examine verb is used. Machines append their
/// current upgrade values as percentage lines.
/// </summary>
public sealed class UpgradeExamineEvent : EntityEventArgs
{
    private readonly FormattedMessage _message;

    public UpgradeExamineEvent(ref FormattedMessage message)
    {
        _message = message;
    }

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier)
    {
        var percent = Math.Round(100 * MathF.Abs(multiplier - 1), 2);
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage",
            1 or float.NaN => "machine-upgrade-not-upgraded",
            > 1 => "machine-upgrade-increased-by-percentage",
        };

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", percent)) + '\n',
            out _);
    }

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier, float timeModifier)
    {
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage-extra",
            1 or float.NaN => "machine-upgrade-not-upgraded-extra",
            > 1 => "machine-upgrade-increased-by-percentage-extra",
        };

        var percentValue = 0f;

        if (float.IsFinite(multiplier) && float.IsFinite(timeModifier) && timeModifier > 0f)
        {
            percentValue = multiplier switch
            {
                < 1 => 100f * timeModifier * MathF.Abs(multiplier - 1f),
                > 1 => 100f / timeModifier * MathF.Abs(multiplier - 1f),
                _ => 100f / timeModifier,
            };

            if (!float.IsFinite(percentValue))
                percentValue = 0f;
        }

        FixedPoint2 percent = percentValue;

        var color = timeModifier switch
        {
            < 1 => "#6DFFA5",
            1 or float.NaN => "#FFFFFF",
            > 1 => "#FF7A7A",
        };

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", percent),
            ("color", color)) + '\n',
            out _);
    }
}
