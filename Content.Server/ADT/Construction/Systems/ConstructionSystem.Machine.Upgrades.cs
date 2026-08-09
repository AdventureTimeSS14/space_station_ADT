// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    private void InitializeMachineUpgrades()
    {
        SubscribeLocalEvent<MachineComponent, GetVerbsEvent<ExamineVerb>>(OnMachineExaminableVerb);
    }

    private void OnMachineExaminableVerb(EntityUid uid, MachineComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        RaiseLocalEvent(uid, new UpgradeExamineEvent(ref markup));

        if (markup.IsEmpty)
            return;

        if (!FormattedMessage.TryFromMarkup(markup.ToMarkup().TrimEnd('\n'), out markup))
            markup = FormattedMessage.Empty;

        if (markup.IsEmpty)
            return;

        args.Verbs.Add(new ExamineVerb
        {
            Act = () => _examineSystem.SendExamineTooltip(args.User, uid, markup, getVerbs: false, centerAtCursor: false),
            Text = Loc.GetString("machine-upgrade-examinable-verb-text"),
            Message = Loc.GetString("machine-upgrade-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        });
    }

    public bool GetMachinePartState(EntityUid uid, out MachinePartState state)
    {
        state = new MachinePartState();

        if (!TryComp(uid, out MachinePartComponent? part))
            return false;

        state.Part = part;
        TryComp(uid, out state.Stack);
        return true;
    }

    private List<MachinePartState> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartState>();
        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (GetMachinePartState(entity, out var partState))
                parts.Add(partState);
        }

        return parts;
    }

    private Dictionary<ProtoId<MachinePartPrototype>, float> GetPartRatings(List<MachinePartState> partStates)
    {
        var weightedRatings = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        var quantities = new Dictionary<ProtoId<MachinePartPrototype>, float>();

        foreach (var state in partStates)
        {
            var id = state.Part.Part;
            var count = state.Quantity();

            quantities[id] = quantities.GetValueOrDefault(id) + count;
            weightedRatings[id] = weightedRatings.GetValueOrDefault(id) + state.Part.Tier * count;
        }

        var result = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        foreach (var (id, quantity) in quantities)
        {
            result[id] = quantity > 0 ? weightedRatings[id] / quantity : 1f;
        }

        return result;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var parts = GetAllParts(component);
        var partRatings = GetPartRatings(parts);

        RaiseLocalEvent(uid,
            new RefreshPartsEvent
            {
                Parts = parts,
                PartRatings = partRatings,
            },
            broadcast: true);
    }
}
