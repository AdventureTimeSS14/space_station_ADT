using System.Linq;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.ADT.Surgery.Prototypes.Conditions;
using Content.Shared.ADT.Surgery.Prototypes.Effects;
using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected IPrototypeManager PrototypeManager = default!;

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

    protected IEnumerable<SurgeryGraphEdge> GetPatientEdges(EntityUid patient, SurgeryGraphNode node)
    {
        foreach (var edge in GetAllEdges(node))
        {
            if (!edge.PerImplant)
            {
                yield return edge;
                continue;
            }

            foreach (var variant in ExpandImplantEdge(patient, edge))
                yield return variant;
        }
    }

    private IEnumerable<SurgeryGraphEdge> ExpandImplantEdge(EntityUid patient, SurgeryGraphEdge edge)
    {
        var template = edge.Effects.OfType<ExtractImplantEffect>().FirstOrDefault();
        if (template == null || !HasImplantCondition.TryGetImplanted(patient, template.OrganId, EntityManager, out var implanted))
            yield break;

        var seen = new HashSet<string>();
        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (!HasImplantCondition.CanExtract(implant, implanted.ImplantContainer, EntityManager, template.Include, template.Exclude))
                continue;

            if (MetaData(implant).EntityPrototype is not { } proto || !seen.Add(proto.ID))
                continue;

            yield return new SurgeryGraphEdge
            {
                Id = $"{edge.Id}:{proto.ID}",
                Target = edge.Target,
                Label = edge.Label,
                DisplayName = proto.Name,
                Steps = edge.Steps,
                Conditions = edge.Conditions
                    .Where(condition => condition is not HasImplantCondition)
                    .Append(new HasImplantCondition { Include = new List<EntProtoId> { proto.ID }, OrganId = template.OrganId })
                    .ToList(),
                Effects = edge.Effects
                    .Where(effect => effect is not ExtractImplantEffect)
                    .Append(new ExtractImplantEffect { Include = new List<EntProtoId> { proto.ID }, OrganId = template.OrganId })
                    .ToList(),
            };
        }
    }

    protected SurgeryGraphEdge? GetEdge(EntityUid patient, SurgeryGraphPrototype graph, string currentNode, string edgeId)
    {
        var node = GetNode(graph, currentNode);
        if (node == null)
            return null;

        foreach (var edge in GetPatientEdges(patient, node))
        {
            if (edge.Id == edgeId)
                return edge;
        }

        return null;
    }

    protected bool EdgeConditionsMet(EntityUid patient, SurgeryGraphEdge edge)
    {
        foreach (var condition in edge.Conditions)
        {
            if (!condition.CheckWithInvert(patient, EntityManager))
                return false;
        }

        return true;
    }

    protected bool HasAvailableEdge(EntityUid patient, SurgeryGraphNode node)
    {
        foreach (var edge in GetPatientEdges(patient, node))
        {
            if (EdgeConditionsMet(patient, edge))
                return true;
        }

        return false;
    }

    protected string EdgeLabel(SurgeryGraphEdge edge)
    {
        return edge.DisplayName ?? LocOrId(edge.Label, edge.Id);
    }

    protected string LocOrId(string locId, string fallback)
    {
        return string.IsNullOrEmpty(locId) ? fallback : Loc.GetString(locId);
    }
}
