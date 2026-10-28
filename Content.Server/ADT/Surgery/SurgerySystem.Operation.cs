using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Robust.Shared.Random;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private void OnSelectGraph(EntityUid uid, BodyComponent body, SurgerySelectGraphMessage args)
    {
        if (!PrototypeManager.TryIndex<SurgeryGraphPrototype>(args.GraphId, out var graph)
            || GetNode(graph, graph.StartNode) is not { } startNode
            || !HasAvailableEdge(uid, startNode))
            return;

        var comp = EnsureComp<OperatedComponent>(uid);

        if (IsBusy(comp) || comp.ActiveEdgeId != null && comp.CompletedSteps > 0)
        {
            _popup.PopupEntity(Loc.GetString("surgery-cant-switch-in-progress"), uid, args.Actor);
            return;
        }

        if (TryGetGraph(comp, out var currentGraph)
            && comp.CurrentNode != currentGraph.StartNode
            && GetNode(currentGraph, comp.CurrentNode) is { } currentNode
            && HasAvailableEdge(uid, currentNode))
        {
            _popup.PopupEntity(Loc.GetString("surgery-cant-switch-in-progress"), uid, args.Actor);
            return;
        }

        comp.GraphId = graph.ID;
        comp.CurrentNode = graph.StartNode;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;
    }

    private void OnSelectEdge(EntityUid uid, OperatedComponent comp, SurgerySelectEdgeMessage args)
    {
        if (IsBusy(comp) || !TryGetGraph(comp, out var graph))
            return;

        var edge = GetEdge(graph, comp.CurrentNode, args.EdgeId);
        if (edge == null || edge.Steps.Count == 0 || !EdgeConditionsMet(uid, edge))
            return;

        if (!_hands.TryGetActiveItem(args.Actor, out var heldItem))
        {
            _popup.PopupEntity(Loc.GetString("surgery-no-tool"), uid, args.Actor);
            return;
        }

        var stepIndex = GetStepIndex(comp, edge);
        if (!TryMatchTool(edge.Steps[stepIndex], heldItem.Value, out var match))
        {
            _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), uid, args.Actor);
            return;
        }

        StartStep(args.Actor, uid, comp, edge, stepIndex, heldItem.Value, match);
    }

    private bool TryPerformStep(EntityUid user, EntityUid target, OperatedComponent comp, EntityUid heldItem, bool silent = false)
    {
        if (IsBusy(comp))
            return !silent;

        if (!TryGetGraph(comp, out var graph))
            return false;

        var node = GetNode(graph, comp.CurrentNode);
        if (node == null)
            return false;

        var matches = new List<SurgeryGraphEdge>();
        var matchStepIndex = 0;
        var toolMatch = ExactMatch;
        var hasEdges = false;
        foreach (var edge in GetAllEdges(node))
        {
            hasEdges = true;

            if (edge.Steps.Count == 0 || !EdgeConditionsMet(target, edge))
                continue;

            var stepIndex = GetStepIndex(comp, edge);
            if (!TryMatchTool(edge.Steps[stepIndex], heldItem, out var match))
                continue;

            matches.Add(edge);
            matchStepIndex = stepIndex;
            toolMatch = match;
        }

        if (!hasEdges)
        {
            if (silent)
                return false;

            _popup.PopupEntity(Loc.GetString("surgery-choose-with-drapes"), target, user, Shared.Popups.PopupType.MediumCaution);
            return true;
        }

        switch (matches.Count)
        {
            case 0:
                if (silent)
                    return false;

                _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), target, user);
                break;

            case 1:
                StartStep(user, target, comp, matches[0], matchStepIndex, heldItem, toolMatch);
                break;

            default:
                OpenEdgeChoice(user, target, matches);
                break;
        }

        return true;
    }

    private static int GetStepIndex(OperatedComponent comp, SurgeryGraphEdge edge)
    {
        return comp.ActiveEdgeId == edge.Id && comp.CompletedSteps < edge.Steps.Count
            ? comp.CompletedSteps
            : 0;
    }

    private void StartStep(EntityUid surgeon, EntityUid patient, OperatedComponent comp, SurgeryGraphEdge edge, int stepIndex, EntityUid heldItem, SurgeryToolMatch match)
    {
        var step = edge.Steps[stepIndex];
        var duration = GetStepDuration(patient, step, match, heldItem);

        comp.ActiveEdgeId = edge.Id;
        comp.CompletedSteps = stepIndex;
        comp.IsOperating = true;

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, surgeon, duration,
            new SurgeryStepDoAfterEvent(edge.Id, stepIndex), patient, target: patient, used: heldItem)
        {
            BreakOnMove = true,
            NeedHand = true,
        }, out var doAfter);

        if (!started)
        {
            comp.IsOperating = false;
            return;
        }

        _audio.PlayPvs(GetStepSound(step, match, heldItem), patient);

        if (comp.IsOperating)
            comp.DoAfter = doAfter;
    }

    private void OnStepDoAfter(EntityUid uid, OperatedComponent comp, SurgeryStepDoAfterEvent args)
    {
        comp.IsOperating = false;
        comp.DoAfter = null;

        if (args.Cancelled || args.Handled)
            return;

        if (comp.ActiveEdgeId != args.EdgeId || comp.CompletedSteps != args.StepIndex)
            return;

        if (!TryGetGraph(comp, out var graph))
            return;

        var edge = GetEdge(graph, comp.CurrentNode, args.EdgeId);
        if (edge == null || args.StepIndex >= edge.Steps.Count || !EdgeConditionsMet(uid, edge))
            return;

        var surgeon = args.User;
        var step = edge.Steps[args.StepIndex];

        var match = ExactMatch;
        if (args.Used is { } used && !TryMatchTool(step, used, out match))
        {
            _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), uid, surgeon);
            args.Handled = true;
            return;
        }

        var successChance = GetSuccessChance(uid, surgeon, step, match);

        if (!IsAnesthetized(uid))
        {
            if (!_mobState.IsIncapacitated(uid))
                _chat.TryEmoteWithChat(uid, ScreamEmote, ChatTransmitRange.Normal, ignoreActionBlocker: true);

            if (!IsWeaklyAnesthetized(uid))
                _damageable.TryChangeDamage(uid, NoAnestheticPainDamage, origin: surgeon);
        }

        if (!_random.Prob(successChance))
        {
            _popup.PopupEntity(Loc.GetString("surgery-step-failed"), uid, surgeon, Shared.Popups.PopupType.MediumCaution);

            foreach (var effect in step.FailureEffects)
                effect.Apply(uid, surgeon, args.Used, EntityManager);

            foreach (var effect in match.FailureEffects)
                effect.Apply(uid, surgeon, args.Used, EntityManager);

            args.Handled = true;
            return;
        }

        comp.CompletedSteps = args.StepIndex + 1;

        foreach (var effect in step.Effects)
            effect.Apply(uid, surgeon, args.Used, EntityManager);

        if (comp.CompletedSteps >= edge.Steps.Count)
        {
            foreach (var effect in edge.Effects)
                effect.Apply(uid, surgeon, args.Used, EntityManager);

            AdvanceNode(comp, graph, edge.Target);
        }

        args.Handled = true;
    }
}
