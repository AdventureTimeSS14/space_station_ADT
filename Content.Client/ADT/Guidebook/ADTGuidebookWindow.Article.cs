using System.Linq;
using Content.Client.ADT.Guidebook.Lazy;
using Content.Client.Guidebook.Controls;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.ControlExtensions;
using Content.Client.UserInterface.Controls.FancyTree;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Guidebook;

public sealed partial class ADTGuidebookWindow
{
    private readonly List<IPrototypeRepresentationControl> _representations = new();
    private readonly List<ISearchableControl> _articleFilters = new();
    private readonly HashSet<IPrototype> _availablePrototypes = new();

    private void ShowEntry(string id)
    {
        var entry = _entries[id];
        SetEntryPath(id);
        UpdateBookmarkButton(id);

        _article = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(2),
        };
        PageBody.AddChild(_article);

        var document = GetDocument(id);
        if (!document.Valid)
        {
            _article.AddChild(CreateText(Loc.GetString("adt-guidebook-document-error")));
            return;
        }

        _parsing.TryAddMarkup(_article, document.Markup);
        EnableArticleLinks();
        _articleFilters.Clear();
        _articleFilters.AddRange(_article.GetSearchableControls());
        FilterTools.Visible = entry.FilterEnabled;
        AddChildList(id);
        BuildOutline();
    }

    private void AddChildList(string id)
    {
        var children = _entries[id].Children
            .Select(child => child.Id)
            .Where(_visibleEntries.Contains)
            .Distinct()
            .ToList();
        if (children.Count == 0)
            return;

        PageBody.AddChild(new PanelContainer
        {
            StyleClasses = { StyleClass.LowDivider },
            Margin = new Thickness(0, 8, 0, 4),
        });
        var header = new Label { Text = Loc.GetString("adt-guidebook-in-section") };
        header.AddStyleClass("LabelHeading");
        PageBody.AddChild(header);
        AddEntryRows(children);
    }

    private void BuildOutline()
    {
        ClearControls(OutlineList);
        if (_article == null)
            return;

        var headings = new List<Label>();
        var controls = new Stack<Control>();
        controls.Push(_article);
        while (controls.TryPop(out var control))
        {
            if (control is Label label
                && (label.HasStyleClass("LabelHeadingBigger") || label.HasStyleClass("LabelHeading")))
            {
                headings.Add(label);
            }

            for (var i = control.ChildCount - 1; i >= 0; i--)
            {
                controls.Push(control.GetChild(i));
            }
        }

        if (headings.Count < 2)
            return;

        var index = 0;
        foreach (var heading in headings)
        {
            var target = heading;
            var sub = heading.HasStyleClass("LabelHeading");
            var button = new ContainerButton
            {
                StyleIdentifier = TreeItem.StyleIdentifierTreeButton,
                HorizontalExpand = true,
            };
            button.AddStyleClass(index++ % 2 == 0 ? TreeItem.StyleClassEvenRow : TreeItem.StyleClassOddRow);
            var label = CreateWrappedLabel(heading.Text ?? string.Empty);
            label.Margin = new Thickness(sub ? 16 : 4, 3, 2, 3);
            button.AddChild(label);
            button.OnPressed += _ => ScrollTo(target);
            OutlineList.AddChild(button);
        }
    }

    private void EnableArticleLinks()
    {
        _representations.Clear();
        _availablePrototypes.Clear();
        if (_article == null)
            return;

        var links = new List<IPrototypeLinkControl>();
        CollectLinks(_article, links, true);
        foreach (var representation in _representations)
        {
            if (representation.RepresentedPrototype is { } prototype)
                _availablePrototypes.Add(prototype);
        }

        EnableLinks(links);
    }

    private void OnLazyMaterialized(Control control)
    {
        if (_article == null || control.Disposed)
            return;

        var links = new List<IPrototypeLinkControl>();
        CollectLinks(control, links, false);
        EnableLinks(links);
    }

    private void CollectLinks(Control root, List<IPrototypeLinkControl> links, bool subscribe)
    {
        var controls = new Stack<Control>();
        controls.Push(root);
        while (controls.TryPop(out var control))
        {
            if (subscribe && control is IPrototypeRepresentationControl representation)
                _representations.Add(representation);
            if (control is IPrototypeLinkControl link)
                links.Add(link);
            if (subscribe && control is ADTLazyList lazy)
                lazy.Materialized += OnLazyMaterialized;

            foreach (var child in control.Children)
            {
                controls.Push(child);
            }
        }
    }

    private void EnableLinks(List<IPrototypeLinkControl> links)
    {
        foreach (var link in links)
        {
            if (link.LinkedPrototype is { } prototype && _availablePrototypes.Contains(prototype))
                link.EnablePrototypeLink();
        }
    }

    private void FilterArticle()
    {
        if (_article == null)
            return;

        var query = FilterBar.Text.Trim();
        foreach (var filter in _articleFilters)
        {
            filter.SetHiddenState(true, query);
        }

        ClearFilterButton.Disabled = query.Length == 0;
    }

    public void HandleAnchor(IPrototypeLinkControl prototypeLinkControl)
    {
        if (prototypeLinkControl.LinkedPrototype is not { } prototype)
            return;

        foreach (var representation in _representations)
        {
            if (representation.RepresentedPrototype != prototype || representation is not Control control)
                continue;

            if (!control.Visible)
                control.Visible = true;

            if (control is ADTLazySlot slot)
                slot.Materialize();
            ScrollTo(control);
            return;
        }
    }

    private void ScrollTo(Control control)
    {
        UserInterfaceManager.DeferAction(() =>
        {
            if (!Disposed && !control.Disposed && control.GetControlScrollPosition() is { } position)
                ContentScroll.VScrollTarget = position.Y;
        });
    }
}
