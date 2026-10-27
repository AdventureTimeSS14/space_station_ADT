using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Random;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private void OnSelectGraph(EntityUid uid, OperatedComponent comp, SurgerySelectGraphMessage args)
    {
        if (!PrototypeManager.TryIndex<SurgeryGraphPrototype>(args.GraphId, out var graph) || graph.StartNodes.Count == 0)
            return;

        if (comp.IsOperating || comp.ActiveEdgeId != null)
        {
            _popup.PopupEntity(Loc.GetString("surgery-cant-switch-in-progress"), uid, args.Actor);
            return;
        }

        if (comp.GraphId is { } currentGraphId
            && !string.IsNullOrEmpty(comp.CurrentNode)
            && PrototypeManager.TryIndex(currentGraphId, out var currentGraph)
            && !currentGraph.StartNodes.Contains(comp.CurrentNode))
        {
            var currentNode = GetNode(currentGraph, comp.CurrentNode);
            var isDeadEnd = true;
            if (currentNode != null)
            {
                foreach (var edge in GetAllEdges(currentNode))
                {
                    if (!EdgeConditionsMet(uid, edge))
                        continue;

                    isDeadEnd = false;
                    break;
                }
            }

            if (!isDeadEnd)
            {
                _popup.PopupEntity(Loc.GetString("surgery-cant-switch-in-progress"), uid, args.Actor);
                return;
            }
        }

        comp.GraphId = args.GraphId;

        comp.CurrentNode = graph.StartNodes[0];
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;
    }

    private void OnSelectEdge(EntityUid uid, OperatedComponent comp, SurgerySelectEdgeMessage args)
    {
        if (comp.IsOperating)
            return;

        if (!TryGetGraph(comp, out var graph))
            return;

        var edge = GetEdge(graph, comp.CurrentNode, args.EdgeId);
        if (edge == null || edge.Steps.Count == 0 || !EdgeConditionsMet(uid, edge))
            return;

        if (!_hands.TryGetActiveItem(args.Actor, out var heldItem))
        {
            _popup.PopupEntity(Loc.GetString("surgery-no-tool"), uid, args.Actor);
            return;
        }

        var stepIndex = comp.ActiveEdgeId == edge.Id && comp.CompletedSteps < edge.Steps.Count
            ? comp.CompletedSteps
            : 0;

        if (!TryMatchTool(edge.Steps[stepIndex], heldItem.Value, out var match))
        {
            _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), uid, args.Actor);
            return;
        }

        comp.ActiveEdgeId = edge.Id;
        comp.CompletedSteps = stepIndex;
        comp.Surgeon = args.Actor;

        StartStep(uid, comp, edge, stepIndex, heldItem.Value, match);
    }

    private bool TryPerformStep(EntityUid user, EntityUid target, OperatedComponent comp, EntityUid heldItem, bool silent = false)
    {
        if (comp.IsOperating)
            return !silent;

        if (!TryGetGraph(comp, out var graph))
            return false;

        var node = GetNode(graph, comp.CurrentNode);
        if (node == null)
            return false;

        var allEdges = new List<SurgeryGraphEdge>(GetAllEdges(node));

        if (allEdges.Count == 0)
        {
            if (silent)
                return false;

            _popup.PopupEntity(Loc.GetString("surgery-choose-with-drapes"), target, user, Shared.Popups.PopupType.MediumCaution);
            return true;
        }

        var matches = new List<SurgeryGraphEdge>();
        var matchStepIndex = 0;
        var toolMatch = ExactMatch;
        foreach (var edge in allEdges)
        {
            if (edge.Steps.Count == 0 || !EdgeConditionsMet(target, edge))
                continue;

            var stepIndex = edge.Id == comp.ActiveEdgeId && comp.CompletedSteps < edge.Steps.Count
                ? comp.CompletedSteps
                : 0;

            if (!TryMatchTool(edge.Steps[stepIndex], heldItem, out var match))
                continue;

            matches.Add(edge);
            matchStepIndex = stepIndex;
            toolMatch = match;
        }

        switch (matches.Count)
        {
            case 0:
                if (silent)
                    return false;

                _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), target, user);
                break;

            case 1:
                comp.ActiveEdgeId = matches[0].Id;
                comp.CompletedSteps = matchStepIndex;
                comp.Surgeon = user;
                StartStep(target, comp, matches[0], matchStepIndex, heldItem, toolMatch);
                break;

            default:
                OpenEdgeChoice(user, target, matches);
                break;
        }

        return true;
    }

    private void StartStep(EntityUid patient, OperatedComponent comp, SurgeryGraphEdge edge, int stepIndex, EntityUid heldItem, SurgeryToolMatch match)
    {
        if (comp.Surgeon is not { } surgeon)
            return;

        var step = edge.Steps[stepIndex];
        var duration = GetStepDuration(patient, step, match, heldItem);

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, surgeon, duration,
            new SurgeryStepDoAfterEvent(edge.Id, stepIndex), patient, target: patient, used: heldItem)
        {
            BreakOnMove = true,
            NeedHand = true,
        });

        if (!started)
            return;

        comp.IsOperating = true;
        _audio.PlayPvs(GetStepSound(step, match, heldItem), patient);
    }

    private void OnStepDoAfter(EntityUid uid, OperatedComponent comp, SurgeryStepDoAfterEvent args)
    {
        comp.IsOperating = false;

        if (args.Cancelled || args.Handled)
            return;

        if (!TryGetGraph(comp, out var graph))
            return;

        var edge = GetEdge(graph, comp.CurrentNode, args.EdgeId);
        if (edge == null || args.StepIndex >= edge.Steps.Count)
            return;

        var step = edge.Steps[args.StepIndex];

        var match = ExactMatch;
        if (args.Used is { } used && !TryMatchTool(step, used, out match))
        {
            _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), uid, comp.Surgeon ?? uid);
            args.Handled = true;
            return;
        }

        var successChance = GetSuccessChance(uid, comp.Surgeon, step, match);

        if (!IsAnesthetized(uid))
        {
            if (!_mobState.IsIncapacitated(uid))
                _chat.TryEmoteWithChat(uid, ScreamEmote, ChatTransmitRange.Normal, ignoreActionBlocker: true);

            if (!IsWeaklyAnesthetized(uid))
                _damageable.TryChangeDamage(uid, NoAnestheticPainDamage, origin: comp.Surgeon);
        }

        if (!_random.Prob(successChance))
        {
            _popup.PopupEntity(Loc.GetString("surgery-step-failed"), uid, comp.Surgeon ?? uid, Shared.Popups.PopupType.MediumCaution);

            foreach (var effect in step.FailureEffects)
                effect.Apply(uid, comp.Surgeon, args.Used, EntityManager);

            foreach (var effect in match.FailureEffects)
                effect.Apply(uid, comp.Surgeon, args.Used, EntityManager);

            args.Handled = true;
            return;
        }

        comp.CompletedSteps = args.StepIndex + 1;

        foreach (var effect in step.Effects)
            effect.Apply(uid, comp.Surgeon, args.Used, EntityManager);

        if (comp.CompletedSteps >= edge.Steps.Count)
        {
            foreach (var effect in edge.Effects)
                effect.Apply(uid, comp.Surgeon, args.Used, EntityManager);

            AdvanceNode(comp, edge.Target);
        }

        args.Handled = true;
    }
}
