using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryGraphValidationTest : GameTest
{
    [SidedDependency(Side.Server)] private ILocalizationManager _loc = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void GraphsAreWellFormed()
    {
        var errors = new List<string>();

        foreach (var graph in SProtoMan.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            ValidateStructure(graph, errors);
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AllNodesAreReachable()
    {
        var errors = new List<string>();

        foreach (var graph in SProtoMan.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            var nodes = graph.Nodes.GroupBy(n => n.Name).ToDictionary(g => g.Key, g => g.First());
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            if (nodes.ContainsKey(graph.StartNode))
                queue.Enqueue(graph.StartNode);

            while (queue.Count > 0)
            {
                var name = queue.Dequeue();
                if (!visited.Add(name))
                    continue;

                foreach (var edge in AllEdges(nodes[name]))
                {
                    if (nodes.ContainsKey(edge.Target))
                        queue.Enqueue(edge.Target);
                }
            }

            foreach (var node in graph.Nodes.Where(n => !visited.Contains(n.Name)))
            {
                errors.Add($"{graph.ID}: node '{node.Name}' is unreachable from the start node");
            }
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void GraphsAreLocalized()
    {
        var errors = new List<string>();

        foreach (var graph in SProtoMan.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            CheckLoc($"{graph.ID}.name", graph.Name, errors);
            CheckLoc($"{graph.ID}.location", graph.Location, errors);

            foreach (var node in graph.Nodes)
            {
                CheckLoc($"{graph.ID}/{node.Name}.label", node.Label, errors);

                foreach (var edge in AllEdges(node))
                {
                    CheckLoc($"{graph.ID}/{node.Name}/{edge.Id}.label", edge.Label, errors);

                    foreach (var step in edge.Steps)
                    {
                        CheckLoc($"{graph.ID}/{node.Name}/{edge.Id}.step", step.Name, errors);
                    }
                }
            }
        }

        foreach (var category in SProtoMan.EnumeratePrototypes<SurgeryCategoryPrototype>())
        {
            CheckLoc($"category {category.ID}.name", category.Name, errors);
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ToolQualitiesAreWellFormed()
    {
        var errors = new List<string>();

        foreach (var quality in SProtoMan.EnumeratePrototypes<SurgeryToolQualityPrototype>())
        {
            CheckLoc($"quality {quality.ID}.name", quality.Name, errors);

            if (quality.Entries.Count == 0)
                errors.Add($"{quality.ID}: no entries");

            for (var i = 0; i < quality.Entries.Count; i++)
            {
                var entry = quality.Entries[i];
                var where = $"{quality.ID}[{i}]";

                if (!entry.HasCriteria)
                    errors.Add($"{where}: entry matches any item");

                if (entry.Speed <= 0f)
                    errors.Add($"{where}: non-positive speed {entry.Speed}");

                if (entry.Success is <= 0f or > 1f)
                    errors.Add($"{where}: success {entry.Success} outside (0, 1]");

                if (entry.ToolQuality is { } toolQuality && !SProtoMan.HasIndex(toolQuality))
                    errors.Add($"{where}: unknown tool quality '{toolQuality}'");

                foreach (var tag in entry.Whitelist?.Tags ?? [])
                {
                    if (!SProtoMan.HasIndex<TagPrototype>(tag))
                        errors.Add($"{where}: unknown tag '{tag}'");
                }
            }
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    private void ValidateStructure(SurgeryGraphPrototype graph, List<string> errors)
    {
        var id = graph.ID;

        if (graph.Category is { } category)
        {
            if (!SProtoMan.HasIndex(category))
                errors.Add($"{id}: unknown category '{category}'");
        }


        var names = new HashSet<string>();
        foreach (var node in graph.Nodes)
        {
            if (!names.Add(node.Name))
                errors.Add($"{id}: duplicate node '{node.Name}'");
        }

        if (!names.Contains(graph.StartNode))
            errors.Add($"{id}: start node '{graph.StartNode}' does not exist");

        foreach (var node in graph.Nodes)
        {
            foreach (var package in node.Packages)
            {
                if (!SProtoMan.HasIndex(package))
                    errors.Add($"{id}/{node.Name}: unknown edge package '{package}'");
            }

            var edgeIds = new HashSet<string>();
            foreach (var edge in AllEdges(node))
            {
                var where = $"{id}/{node.Name}/{edge.Id}";

                if (!edgeIds.Add(edge.Id))
                    errors.Add($"{where}: duplicate edge id");

                if (!names.Contains(edge.Target))
                    errors.Add($"{where}: target '{edge.Target}' does not exist");

                if (edge.Steps.Count == 0)
                    errors.Add($"{where}: edge has no steps");

                foreach (var step in edge.Steps)
                {
                    if (step.Quality is { } quality && !SProtoMan.HasIndex(quality))
                        errors.Add($"{where}: unknown tool quality '{quality}'");

                    if (step.Quality == null && step.Organ == null)
                        errors.Add($"{where}: step '{step.Name}' has neither quality nor organ");

                    if (step.Quality != null && step.Organ != null)
                        errors.Add($"{where}: step '{step.Name}' has both quality and organ");

                    if (step.Organ is { } organ && !SProtoMan.HasIndex(organ))
                        errors.Add($"{where}: unknown organ category '{organ}'");

                    if (step.Duration <= TimeSpan.Zero)
                        errors.Add($"{where}: step '{step.Name}' has non-positive duration");

                    if (step.SuccessChance is <= 0f or > 1f)
                        errors.Add($"{where}: step '{step.Name}' has success chance {step.SuccessChance} outside (0, 1]");
                }
            }
        }
    }

    private IEnumerable<SurgeryGraphEdge> AllEdges(SurgeryGraphNode node)
    {
        foreach (var edge in node.Edges)
        {
            yield return edge;
        }

        foreach (var packageId in node.Packages)
        {
            if (!SProtoMan.TryIndex(packageId, out var package))
                continue;

            foreach (var edge in package.Edges)
            {
                yield return edge;
            }
        }
    }

    private void CheckLoc(string where, string key, List<string> errors)
    {
        if (string.IsNullOrEmpty(key))
        {
            errors.Add($"{where}: empty loc key");
            return;
        }

        if (!_loc.HasString(key))
            errors.Add($"{where}: missing localization '{key}'");
    }
}
