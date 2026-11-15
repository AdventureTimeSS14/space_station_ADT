// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Objectives.Components;
using Content.Shared.Vampire.Components;
using Content.Server.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

public sealed partial class DiablerieConditionSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiablerieConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<DiablerieConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<DiablerieConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAssigned(EntityUid uid, DiablerieConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (args.Mind.OwnedEntity.HasValue)
        {
            var ownedEntity = args.Mind.OwnedEntity.Value;
            comp.BloodTargets[ownedEntity] = _random.Next(1, 1);
        }
    }

    private void OnAfterAssign(EntityUid uid, DiablerieConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (args.Mind.OwnedEntity.HasValue)
        {
            var ownedEntity = args.Mind.OwnedEntity.Value;
            var description = Loc.GetString("objective-dilebry-description", ("condition", comp.BloodTargets[ownedEntity]));
            _metaData.SetEntityDescription(uid, description, args.Meta);
        }
    }

    private void OnGetProgress(EntityUid uid, DiablerieConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity.HasValue)
        {
            var ownedEntity = args.Mind.OwnedEntity.Value;
            args.Progress = GetProgress(ownedEntity, comp);
        }
    }

    private float GetProgress(EntityUid uid, DiablerieConditionComponent comp)
    {
        if (!TryComp<VampireDiablerieComponent>(uid, out var vampireComponent))
            return 0f;

        float targetBlood = comp.BloodTargets.GetValueOrDefault(uid, 0);
        float bloodDrank = vampireComponent.DiablerieLevel;

        return bloodDrank >= targetBlood ? 1f : bloodDrank / targetBlood;
    }
}
