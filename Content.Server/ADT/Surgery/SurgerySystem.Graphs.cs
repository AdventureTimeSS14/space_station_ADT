using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private bool TryGetGraph(OperatedComponent comp, out SurgeryGraphPrototype graph)
    {
        graph = default!;

        if (comp.GraphId is not { } graphId)
            return false;

        return PrototypeManager.TryIndex(graphId, out graph!);
    }

    private void AdvanceNode(OperatedComponent comp, SurgeryGraphPrototype graph, string targetNode)
    {
        comp.CurrentNode = targetNode;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;

        if (targetNode == graph.StartNode || GetNode(graph, targetNode) is not { } node || node.Edges.Count == 0 && node.Packages.Count == 0)
            comp.GraphId = null;
    }

    private bool IsBusy(OperatedComponent comp)
    {
        if (comp.IsOperating && !_doAfter.IsRunning(comp.DoAfter))
        {
            comp.IsOperating = false;
            comp.DoAfter = null;
        }

        return comp.IsOperating;
    }
}
