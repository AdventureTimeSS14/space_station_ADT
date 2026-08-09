// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Steps;

/// <summary>
/// Construction graph step that accepts any stock part of the given machine part type.
/// </summary>
[DataDefinition]
public sealed partial class MachinePartConstructionGraphStep : ArbitraryInsertConstructionGraphStep
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> MachinePart;

    /// <summary>
    /// Optional tier requirement (e.g. tier 4 = bluespace parts only).
    /// </summary>
    [DataField]
    public int? Tier;

    public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
    {
        if (!entityManager.TryGetComponent(uid, out MachinePartComponent? machinePart) || machinePart.Part != MachinePart)
            return false;

        return Tier is not { } tier || machinePart.Tier == tier;
    }

    public override void DoExamine(ExaminedEvent examinedEvent)
    {
        var localizedPartName = MachinePart.Id;
        if (IoCManager.Resolve<IPrototypeManager>().TryIndex(MachinePart, out var machinePartProto) && !string.IsNullOrWhiteSpace(machinePartProto.Name))
            localizedPartName = Loc.GetString(machinePartProto.Name);

        examinedEvent.PushMarkup(string.IsNullOrEmpty(Name)
            ? Loc.GetString("construction-insert-entity-with-component", ("componentName", localizedPartName))
            : Loc.GetString("construction-insert-exact-entity", ("entityName", Loc.GetString(Name))));
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "construction-presenter-arbitrary-step",
            Arguments = [("name", MachinePart.Id)],
            Icon = Icon,
        };
    }
}
