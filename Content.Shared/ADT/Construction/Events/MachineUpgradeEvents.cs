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
    public Dictionary<ProtoId<MachinePartPrototype>, float> PartRatings = new();
    public Dictionary<MachineStat, float> StatMultipliers = new();

    public float GetPartRating(ProtoId<MachinePartPrototype> partId, float defaultValue = 1f)
    {
        return PartRatings.GetValueOrDefault(partId, defaultValue);
    }

    public float GetStatMultiplier(MachineStat stat)
    {
        return StatMultipliers.GetValueOrDefault(stat, 1f);
    }

    /// <summary>
    /// Multiplier of a part type by its average tier: 1 + value per tier above 1.
    /// </summary>
    public static float GetTierMultiplier(float tier, float valuePerTier)
    {
        return 1f + valuePerTier * (tier - 1f);
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

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier, bool benefit)
    {
        if (multiplier is 1f or float.NaN)
        {
            _message.TryAddMarkup(Loc.GetString("machine-upgrade-not-upgraded-extra",
                ("upgraded", Loc.GetString(upgradedLocId)),
                ("percent", FixedPoint2.Zero),
                ("color", "#FFFFFF")) + '\n',
                out _);
            return;
        }

        var increased = multiplier > 1f;
        var good = increased == benefit;

        var locId = increased
            ? "machine-upgrade-increased-by-percentage-extra"
            : "machine-upgrade-decreased-by-percentage-extra";

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", (FixedPoint2) Math.Round(100 * MathF.Abs(multiplier - 1), 2)),
            ("color", good ? "#6DFFA5" : "#FF7A7A")) + '\n',
            out _);
    }
}
