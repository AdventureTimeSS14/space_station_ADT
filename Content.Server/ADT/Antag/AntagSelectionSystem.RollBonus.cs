using Content.Server.GameTicking;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    private static ProtoId<AntagPrototype>? GetRollBonusRole(AntagSpecifierPrototype antag)
    {
        if (antag.PrefRoles.Count > 0)
            return antag.PrefRoles[0];

        return null;
    }

    private bool IsRollBonusTracked()
    {
        return _rollBonus.Enabled && GameTicker.RunLevel != GameRunLevel.InRound;
    }

    private float GetRollBonusWeight(ICommonSession player, IEnumerable<AntagSpecifierPrototype>? antags)
    {
        if (antags == null || !_rollBonus.Enabled)
            return 1f;

        var weight = 1f;
        foreach (var antag in antags)
        {
            if (GetRollBonusRole(antag) is { } role)
                weight = MathF.Max(weight, _rollBonus.GetWeight(player, role));
        }

        return weight;
    }

    private void MarkRollBonusEligible(IEnumerable<ICommonSession> players, List<AntagRule>? rules)
    {
        if (rules == null || !IsRollBonusTracked())
            return;

        foreach (var rule in rules)
        {
            if (!rule.Definition.PickPlayer || GetRollBonusRole(rule.Definition) is not { } role)
                continue;

            var eligible = new List<ICommonSession>();
            foreach (var player in players)
            {
                if (CanBeAntag(player, rule.GameRule, rule.Definition))
                    eligible.Add(player);
            }

            _rollBonus.MarkEligible(eligible, role);
        }
    }

    private void MarkRollBonusPreSelected(ICommonSession player, ProtoId<AntagSpecifierPrototype> protoId)
    {
        if (!IsRollBonusTracked() || !ProtoMan.Resolve(protoId, out var antag))
            return;

        if (GetRollBonusRole(antag) is { } role)
            _rollBonus.MarkPreSelected(player, role);
    }

    private void MarkRollBonusBecameAntag(ICommonSession player, AntagSpecifierPrototype antag)
    {
        if (GetRollBonusRole(antag) is { } role)
            _rollBonus.MarkBecameAntag(player, role);
    }
}
