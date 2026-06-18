// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Server.ADT.BloodBrothers.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.ADT.BloodBrothers.Objectives.Systems;

public sealed class BothBrothersEscapeConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BothBrothersEscapeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BothBrothersEscapeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(comp);
    }

    private float GetProgress(BothBrothersEscapeConditionComponent comp)
    {
        var allEscaped = true;
        var anyPartially = false;

        foreach (var memberMind in comp.TeamMinds)
        {
            if (!_mind.TryGetMind(memberMind, out var _, out var mindComp))
            {
                allEscaped = false;
                continue;
            }

            if (mindComp.OwnedEntity == null || _mind.IsCharacterDeadIc(mindComp))
            {
                allEscaped = false;
                continue;
            }

            var entity = mindComp.OwnedEntity.Value;
            if (!_emergencyShuttle.IsTargetEscaping(entity))
            {
                allEscaped = false;
                continue;
            }

            if (TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0)
                anyPartially = true;
        }

        if (!allEscaped)
            return 0f;

        return anyPartially ? 0.5f : 1f;
    }
}
