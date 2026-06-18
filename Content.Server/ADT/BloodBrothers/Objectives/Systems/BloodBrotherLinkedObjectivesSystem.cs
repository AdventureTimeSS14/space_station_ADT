// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Server.ADT.BloodBrothers.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;

namespace Content.Server.ADT.BloodBrothers.Objectives.Systems;

public sealed class BloodBrotherLinkedObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    private readonly HashSet<(EntityUid Objective, EntityUid Mind)> _processing = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherLinkedObjectivesComponent, ObjectiveGetProgressEvent>(OnGetProgress,
            after: new[]
            {
                typeof(StealConditionSystem),
                typeof(KillPersonConditionSystem),
                typeof(KeepAliveConditionSystem),
            });
    }

    private void OnGetProgress(EntityUid uid, BloodBrotherLinkedObjectivesComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var key = (uid, args.MindId);
        if (!_processing.Add(key))
            return;

        try
        {
            var maxProgress = args.Progress ?? 0f;

            foreach (var teamMindId in comp.TeamMinds)
            {
                if (teamMindId == args.MindId)
                    continue;

                if (!TryComp<MindComponent>(teamMindId, out var teamMind))
                    continue;

                var partnerKey = (uid, teamMindId);
                if (_processing.Contains(partnerKey))
                    continue;

                var progress = _objectives.GetProgress(uid, (teamMindId, teamMind));
                if (progress != null)
                    maxProgress = MathF.Max(maxProgress, progress.Value);
            }

            args.Progress = maxProgress;
        }
        finally
        {
            _processing.Remove(key);
        }
    }
}
