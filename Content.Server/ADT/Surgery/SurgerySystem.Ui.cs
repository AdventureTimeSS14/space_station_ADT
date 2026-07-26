using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.ADT.Surgery.Prototypes.Effects;
using Content.Shared.Body;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private static readonly SpriteSpecifier RemoveOrganOverlayIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/ADT/Interface/Surgery/remove_organ.png"));

    private void OpenGraphChoice(EntityUid user, EntityUid target)
    {
        TryComp<OperatedComponent>(target, out var comp);

        var options = new List<(string, string, string, string)>();
        foreach (var graph in PrototypeManager.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            if (graph.StartNodes.Count == 0)
                continue;

            var currentNodeName = comp != null && comp.GraphId == graph.ID && !string.IsNullOrEmpty(comp.CurrentNode)
                ? comp.CurrentNode
                : graph.StartNodes[0];

            var node = GetNode(graph, currentNodeName);
            if (node == null)
                continue;

            var hasValidEdge = false;
            foreach (var edge in GetAllEdges(node))
            {
                if (!EdgeConditionsMet(target, edge))
                    continue;

                hasValidEdge = true;
                break;
            }

            if (!hasValidEdge)
                continue;

            var name = string.IsNullOrEmpty(graph.Name) ? graph.ID : Loc.GetString(graph.Name);

            var categoryName = string.Empty;
            var subcategoryName = string.Empty;
            if (graph.Category is { } categoryId && PrototypeManager.TryIndex(categoryId, out var category))
            {
                if (category.Parent is { } parentId && PrototypeManager.TryIndex(parentId, out var parentCategory))
                {
                    categoryName = string.IsNullOrEmpty(parentCategory.Name) ? parentCategory.ID : Loc.GetString(parentCategory.Name);
                    subcategoryName = string.IsNullOrEmpty(category.Name) ? category.ID : Loc.GetString(category.Name);
                }
                else
                {
                    categoryName = string.IsNullOrEmpty(category.Name) ? category.ID : Loc.GetString(category.Name);
                }
            }

            options.Add((graph.ID, name, categoryName, subcategoryName));
        }

        if (options.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("surgery-no-graph"), target, user);
            return;
        }

        _ui.SetUi(target, SurgeryUiKey.Key, new InterfaceData("SurgeryBoundUserInterface"));
        _ui.OpenUi(target, SurgeryUiKey.Key, user);
        _ui.SetUiState(target, SurgeryUiKey.Key, new SurgeryChooseGraphState(options));
    }

    private void OpenEdgeChoice(EntityUid user, EntityUid target, List<SurgeryGraphEdge> matches)
    {
        var options = new List<(string, string, NetEntity?, SpriteSpecifier?)>();
        foreach (var edge in matches)
        {
            var (iconEntity, icon) = GetEdgeIcon(target, edge);
            options.Add((edge.Id, string.IsNullOrEmpty(edge.Label) ? edge.Id : Loc.GetString(edge.Label), iconEntity, icon));
        }

        _ui.SetUi(target, SurgeryUiKey.Key, new InterfaceData("SurgeryBoundUserInterface"));
        _ui.OpenUi(target, SurgeryUiKey.Key, user);
        _ui.SetUiState(target, SurgeryUiKey.Key, new SurgeryChooseEdgeState(options));
    }

    private (NetEntity?, SpriteSpecifier?) GetEdgeIcon(EntityUid target, SurgeryGraphEdge edge)
    {
        foreach (var effect in edge.Effects)
        {
            if (effect is not RemoveOrganEffect removeOrgan)
                continue;

            if (!_body.TryGetOrgansWithComponent<OrganComponent>(target, out var organs))
                break;

            foreach (var organ in organs)
            {
                if (organ.Comp.Category != removeOrgan.OrganId)
                    continue;

                return (GetNetEntity(organ.Owner), RemoveOrganOverlayIcon);
            }

            break;
        }

        return (null, edge.Icon);
    }
}
