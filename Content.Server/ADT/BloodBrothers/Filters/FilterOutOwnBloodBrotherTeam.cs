// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Server.ADT.BloodBrothers.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;

namespace Content.Server.ADT.BloodBrothers.Filters;

public sealed partial class FilterOutOwnBloodBrotherTeam : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        if (exclude == null)
            return false;

        var query = entMan.EntityQueryEnumerator<BloodBrotherRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComp))
        {
            foreach (var team in ruleComp.Teams)
            {
                if (!team.MemberMinds.Contains(exclude.Value))
                    continue;

                return team.MemberMinds.Contains(mind.Owner);
            }
        }

        return false;
    }
}
