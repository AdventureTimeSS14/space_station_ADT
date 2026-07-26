using Content.Shared.ADT.Surgery.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery;

public abstract class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    protected static SurgeryGraphNode? GetNode(SurgeryGraphPrototype graph, string name)
    {
        return graph.Nodes.Find(n => n.Name == name);
    }

    protected IEnumerable<SurgeryGraphEdge> GetAllEdges(SurgeryGraphNode node)
    {
        foreach (var edge in node.Edges)
            yield return edge;

        foreach (var packageId in node.Packages)
        {
            if (!PrototypeManager.TryIndex(packageId, out var package))
                continue;

            foreach (var edge in package.Edges)
                yield return edge;
        }
    }

    protected SurgeryGraphEdge? GetEdge(SurgeryGraphPrototype graph, string currentNode, string edgeId)
    {
        var node = GetNode(graph, currentNode);
        if (node == null)
            return null;

        foreach (var edge in GetAllEdges(node))
        {
            if (edge.Id == edgeId)
                return edge;
        }

        return null;
    }
}
