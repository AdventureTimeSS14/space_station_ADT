using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    public bool TryGetOperationState(EntityUid patient, out string graphId, out string node, out List<(string Id, string Target, bool Available)> edges)
    {
        graphId = string.Empty;
        node = string.Empty;
        edges = new();

        if (!TryComp<OperatedComponent>(patient, out var comp) || !TryGetGraph(comp, out var graph))
            return false;

        graphId = graph.ID;
        node = comp.CurrentNode;

        if (GetNode(graph, comp.CurrentNode) is not { } current)
            return true;

        foreach (var edge in GetPatientEdges(patient, current))
        {
            edges.Add((edge.Id, edge.Target, EdgeConditionsMet(patient, edge)));
        }

        return true;
    }

    public bool TrySetGraph(EntityUid patient, string? graphId, string? node)
    {
        if (graphId == null)
        {
            if (TryComp<OperatedComponent>(patient, out var existing))
            {
                _doAfter.Cancel(existing.DoAfter);
                RemComp<OperatedComponent>(patient);
            }

            return true;
        }

        if (!PrototypeManager.TryIndex<SurgeryGraphPrototype>(graphId, out var graph))
            return false;

        var target = node ?? graph.StartNode;
        if (GetNode(graph, target) == null)
            return false;

        var comp = EnsureComp<OperatedComponent>(patient);
        _doAfter.Cancel(comp.DoAfter);
        comp.IsOperating = false;
        comp.DoAfter = null;
        comp.GraphId = graph.ID;
        comp.CurrentNode = target;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;
        return true;
    }

    public bool TryForceEdge(EntityUid patient, string edgeId, EntityUid? surgeon)
    {
        if (!TryComp<OperatedComponent>(patient, out var comp) || !TryGetGraph(comp, out var graph))
            return false;

        var edge = GetEdge(patient, graph, comp.CurrentNode, edgeId);
        if (edge == null || !EdgeConditionsMet(patient, edge))
            return false;

        _doAfter.Cancel(comp.DoAfter);
        comp.IsOperating = false;
        comp.DoAfter = null;

        foreach (var step in edge.Steps)
        {
            foreach (var effect in step.Effects)
                effect.Apply(patient, surgeon, null, EntityManager);
        }

        foreach (var effect in edge.Effects)
            effect.Apply(patient, surgeon, null, EntityManager);

        AdvanceNode(comp, graph, edge.Target);
        return true;
    }
}
