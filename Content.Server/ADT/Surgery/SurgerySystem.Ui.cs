using System.Linq;
using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private void OpenGraphChoice(EntityUid user, EntityUid target)
    {
        TryComp<OperatedComponent>(target, out var comp);

        var options = new List<(string, string, string, string)>();
        foreach (var graph in PrototypeManager.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            var currentNodeName = comp != null && comp.GraphId == graph.ID ? comp.CurrentNode : graph.StartNode;

            var node = GetNode(graph, currentNodeName);
            if (node == null || !HasAvailableEdge(target, node))
                continue;

            var categoryName = string.Empty;
            var subcategoryName = string.Empty;
            if (graph.Category is { } categoryId && PrototypeManager.TryIndex(categoryId, out var category))
            {
                if (category.Parent is { } parentId && PrototypeManager.TryIndex(parentId, out var parentCategory))
                {
                    categoryName = LocOrId(parentCategory.Name, parentCategory.ID);
                    subcategoryName = LocOrId(category.Name, category.ID);
                }
                else
                {
                    categoryName = LocOrId(category.Name, category.ID);
                }
            }

            options.Add((graph.ID, LocOrId(graph.Name, graph.ID), categoryName, subcategoryName));
        }

        if (options.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("surgery-no-graph"), target, user);
            return;
        }

        OpenUi(user, target, new SurgeryChooseGraphState(options));
    }

    private void OpenEdgeChoice(EntityUid user, EntityUid target, List<SurgeryGraphEdge> matches)
    {
        var options = new List<(string, string)>();
        foreach (var edge in matches)
        {
            options.Add((edge.Id, EdgeLabel(edge)));
        }

        OpenUi(user, target, new SurgeryChooseEdgeState(options));
    }

    private void OpenUi(EntityUid user, EntityUid target, BoundUserInterfaceState state)
    {
        _ui.SetUi(target, SurgeryUiKey.Key, new InterfaceData("SurgeryBoundUserInterface"));

        foreach (var actor in _ui.GetActors(target, SurgeryUiKey.Key).ToList())
        {
            if (actor != user)
                _ui.CloseUi(target, SurgeryUiKey.Key, actor);
        }

        _ui.OpenUi(target, SurgeryUiKey.Key, user);
        _ui.SetUiState(target, SurgeryUiKey.Key, state);
    }
}
