using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
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
        if (comp.IsOperating || comp.ActiveEdgeId != null)
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

        if (!ToolMatches(edge.Steps[0], heldItem.Value))
        {
            _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), uid, args.Actor);
            return;
        }

        comp.ActiveEdgeId = edge.Id;
        comp.CompletedSteps = 0;
        comp.Surgeon = args.Actor;

        StartStep(uid, comp, edge, 0, heldItem.Value);
    }

    private void TryPerformStep(EntityUid user, EntityUid target, OperatedComponent comp)
    {
        if (comp.IsOperating)
            return;

        if (!TryGetGraph(comp, out var graph))
            return;

        if (!_hands.TryGetActiveItem(user, out var heldItem))
        {
            _popup.PopupEntity(Loc.GetString("surgery-no-tool"), target, user);
            return;
        }

        if (comp.ActiveEdgeId is { } activeEdgeId)
        {
            var activeEdge = GetEdge(graph, comp.CurrentNode, activeEdgeId);
            if (activeEdge == null || comp.CompletedSteps >= activeEdge.Steps.Count)
                return;

            var nextStep = activeEdge.Steps[comp.CompletedSteps];
            if (!ToolMatches(nextStep, heldItem.Value))
            {
                _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), target, user);
                return;
            }

            comp.Surgeon = user;
            StartStep(target, comp, activeEdge, comp.CompletedSteps, heldItem.Value);
            return;
        }

        var node = GetNode(graph, comp.CurrentNode);
        if (node == null)
            return;

        var allEdges = new List<SurgeryGraphEdge>(GetAllEdges(node));

        if (allEdges.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("surgery-choose-with-drapes"), target, user, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        var matches = new List<SurgeryGraphEdge>();
        foreach (var edge in allEdges)
        {
            if (edge.Steps.Count == 0 || !EdgeConditionsMet(target, edge))
                continue;

            if (!ToolMatches(edge.Steps[0], heldItem.Value))
                continue;

            matches.Add(edge);
        }

        switch (matches.Count)
        {
            case 0:
                _popup.PopupEntity(Loc.GetString("surgery-wrong-tool"), target, user);
                break;

            case 1:
                comp.ActiveEdgeId = matches[0].Id;
                comp.CompletedSteps = 0;
                comp.Surgeon = user;
                StartStep(target, comp, matches[0], 0, heldItem.Value);
                break;

            default:
                OpenEdgeChoice(user, target, matches);
                break;
        }
    }

    private void StartStep(EntityUid patient, OperatedComponent comp, SurgeryGraphEdge edge, int stepIndex, EntityUid heldItem)
    {
        if (comp.Surgeon is not { } surgeon)
            return;

        var step = edge.Steps[stepIndex];

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, surgeon, step.Duration,
            new SurgeryStepDoAfterEvent(edge.Id, stepIndex), patient, target: patient, used: heldItem)
        {
            BreakOnMove = true,
            NeedHand = true,
        });

        if (!started)
            return;

        comp.IsOperating = true;
        _audio.PlayPvs(step.Sound, patient);
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

        if (!_random.Prob(step.SuccessChance))
        {
            _popup.PopupEntity(Loc.GetString("surgery-step-failed"), uid, comp.Surgeon ?? uid);

            foreach (var effect in step.FailureEffects)
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
