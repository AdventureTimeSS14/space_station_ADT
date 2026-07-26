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

    private bool EdgeConditionsMet(EntityUid patient, SurgeryGraphEdge edge)
    {
        foreach (var condition in edge.Conditions)
        {
            if (!condition.CheckWithInvert(patient, EntityManager))
                return false;
        }

        return true;
    }

    private void AdvanceNode(OperatedComponent comp, string targetNode)
    {
        comp.CurrentNode = targetNode;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;
    }

    private bool ToolMatches(SurgeryStepEntry step, EntityUid item)
    {
        if (step.Tools.Count == 0)
            return true;

        foreach (var tag in step.Tools)
        {
            if (_tag.HasTag(item, tag))
                return true;
        }

        return false;
    }
}
