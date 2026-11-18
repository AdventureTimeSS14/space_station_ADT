using System.Linq;
using System.Numerics;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls.FancyTree;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Guidebook;

public sealed partial class ADTGuidebookWindow
{
    private const int TreeIndent = 14;
    private const int MaxTreeDepth = 8;

    private readonly HashSet<string> _expanded = new();
    private int _treeRowIndex;

    private void BuildSidebar()
    {
        _categoryIds.Clear();
        _expanded.Clear();
        CategoryMenu.Clear();
        CategoryMenu.AddItem(Loc.GetString("adt-guidebook-select-category"), 0);

        foreach (var category in _categories.Values)
        {
            _categoryIds.Add(category.Id);
            CategoryMenu.AddItem(category.Name, _categoryIds.Count);
        }

        RebuildSidebar();
    }

    private void RebuildSidebar()
    {
        ClearControls(CategoryList);
        _treeRowIndex = 0;
        var page = _page;
        var active = page == null ? string.Empty : page.Kind == PageKind.Category ? page.Id : page.Category;
        var selected = page is { Kind: PageKind.Entry } ? page.Id : null;

        foreach (var category in _categories.Values)
        {
            var isActive = category.Id == active;
            var single = category.Roots.Count == 1 ? category.Roots[0] : null;
            CategoryList.AddChild(CreateCategoryButton(category, isActive));
            if (!isActive)
                continue;

            var roots = single != null ? GetTreeChildren(single, category) : category.Roots;
            AddTreeRows(category, roots, 0, selected);
        }
    }

    private void AddTreeRows(Category category, IEnumerable<string> ids, int depth, string? selected)
    {
        if (depth > MaxTreeDepth)
            return;

        foreach (var id in ids.Distinct())
        {
            if (!_visibleEntries.Contains(id) || _entryCategories.GetValueOrDefault(id) != category.Id)
                continue;

            var children = GetTreeChildren(id, category);
            var expanded = children.Count > 0 && _expanded.Contains(id);
            CategoryList.AddChild(CreateTreeRow(id, depth, children.Count > 0, expanded, id == selected));
            if (expanded)
                AddTreeRows(category, children, depth + 1, selected);
        }
    }

    private List<string> GetTreeChildren(string id, Category category)
    {
        return _entries[id].Children
            .Select(child => child.Id)
            .Where(child => _visibleEntries.Contains(child) && _entryCategories.GetValueOrDefault(child) == category.Id)
            .Distinct()
            .ToList();
    }

    private void ExpandPathTo(string id)
    {
        _expanded.Clear();
        var visited = new HashSet<string>();
        var current = id;
        _expanded.Add(current);
        while (_parents.TryGetValue(current, out var parent) && visited.Add(parent))
        {
            _expanded.Add(parent);
            current = parent;
        }
    }

    private void TreeRowPressed(string id)
    {
        if (_page is { Kind: PageKind.Entry } page && page.Id == id)
        {
            if (!_expanded.Remove(id))
                _expanded.Add(id);

            RebuildSidebar();
            return;
        }

        OpenEntry(id);
    }

    private Control CreateTreeRow(string id, int depth, bool hasChildren, bool expanded, bool selected)
    {
        var name = GetEntryName(id);
        var button = new ContainerButton
        {
            StyleIdentifier = TreeItem.StyleIdentifierTreeButton,
            HorizontalExpand = true,
            ToolTip = name,
        };
        button.AddStyleClass(_treeRowIndex++ % 2 == 0 ? TreeItem.StyleClassEvenRow : TreeItem.StyleClassOddRow);
        if (selected)
            button.AddStyleClass(TreeItem.StyleClassSelected);

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Margin = new Thickness(6 + depth * TreeIndent, 3, 2, 3),
            MouseFilter = MouseFilterMode.Ignore,
        };
        row.AddChild(new TextureRect
        {
            Texture = hasChildren ? GetTreeIcon(expanded) : null,
            Stretch = TextureRect.StretchMode.KeepCentered,
            SetSize = new Vector2(12, 12),
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(0, 3, 0, 0),
            MouseFilter = MouseFilterMode.Ignore,
        });
        row.AddChild(CreateWrappedLabel(name));
        button.AddChild(row);
        button.OnPressed += _ => TreeRowPressed(id);
        return button;
    }

    private Texture GetTreeIcon(bool expanded)
    {
        return _cache.GetTexture(expanded ? FancyTree.DefaultIconExpanded : FancyTree.DefaultIconCollapsed);
    }

    private ContainerButton CreateCategoryButton(Category category, bool active)
    {
        var button = new ContainerButton
        {
            StyleClasses = { ContainerButton.StyleClassButton },
            HorizontalExpand = true,
            ToolTip = category.Description,
            ToggleMode = true,
            Pressed = active,
        };
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Margin = new Thickness(4, 2),
            MouseFilter = MouseFilterMode.Ignore,
        };
        row.AddChild(new TextureRect
        {
            Texture = _cache.GetTexture(category.Icon),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(20, 20),
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        });
        row.AddChild(CreateWrappedLabel(category.Name));
        button.AddChild(row);
        button.OnPressed += _ =>
        {
            button.Pressed = true;
            OpenCategory(category.Id);
        };
        return button;
    }

    private void ShowHome()
    {
        SetTitle(Loc.GetString("adt-guidebook-title"));
        SetDescription(Loc.GetString("adt-guidebook-home-description"));

        foreach (var category in _categories.Values)
        {
            PageBody.AddChild(CreateCard(category.Name, category.Description, () => OpenCategory(category.Id), category));
        }
    }

    private void ShowCategory(string id)
    {
        if (!_categories.TryGetValue(id, out var category))
            return;

        SetTitle(category.Name);
        SetDescription(category.Description);
        AddEntryRows(category.Roots);
    }

    private void ShowBookmarks()
    {
        SetTitle(Loc.GetString("adt-guidebook-bookmarks"));
        SetDescription(Loc.GetString("adt-guidebook-bookmarks-description"));
        var entries = _bookmarks.Where(_visibleEntries.Contains).OrderBy(GetEntryName).ToList();
        if (entries.Count == 0)
        {
            PageBody.AddChild(CreateText(Loc.GetString("adt-guidebook-bookmarks-empty")));
            return;
        }

        AddEntryRows(entries);
    }

    private void OpenCategory(string id)
    {
        if (!_categories.TryGetValue(id, out var category))
            return;

        if (category.Roots.Count == 1)
            OpenEntry(category.Roots[0]);
        else
            Navigate(new Page(PageKind.Category, id));
    }

    private void AddEntryRows(IEnumerable<string> ids, Control? parent = null)
    {
        parent ??= PageBody;
        foreach (var id in ids.Distinct())
        {
            if (!_entries.ContainsKey(id) || !_visibleEntries.Contains(id))
                continue;

            parent.AddChild(CreateCard(GetEntryName(id), GetDocument(id).Preview, () => OpenEntry(id)));
        }
    }

    private ContainerButton CreateCard(string title, string description, Action pressed, Category? category = null)
    {
        var button = new ContainerButton
        {
            StyleClasses = { ContainerButton.StyleClassButton },
            HorizontalExpand = true,
            ToolTip = title,
        };
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = new Thickness(6, 4),
            MouseFilter = MouseFilterMode.Ignore,
        };
        if (category != null)
        {
            row.AddChild(new TextureRect
            {
                Texture = _cache.GetTexture(category.Icon),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                SetSize = new Vector2(28, 28),
                VerticalAlignment = VAlignment.Center,
                MouseFilter = MouseFilterMode.Ignore,
            });
        }

        var column = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            MouseFilter = MouseFilterMode.Ignore,
        };
        var label = CreateClippedLabel(title);
        label.AddStyleClass(ContainerButton.StyleClassButton);
        column.AddChild(label);
        if (!string.IsNullOrWhiteSpace(description))
        {
            var sub = CreateClippedLabel(description);
            sub.AddStyleClass(StyleClass.LabelSubText);
            column.AddChild(sub);
        }

        row.AddChild(column);
        button.AddChild(row);
        button.OnPressed += _ => pressed();
        return button;
    }

    private Control CreateSearchResult(string id, string snippet, Action pressed)
    {
        var result = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            HorizontalExpand = true,
        };
        result.AddChild(CreateCard(GetEntryName(id), GetEntryPath(id), pressed));
        result.AddChild(CreateText(snippet));
        result.AddChild(new PanelContainer
        {
            StyleClasses = { StyleClass.LowDivider },
            Margin = new Thickness(0, 4),
        });
        return result;
    }

    private void SetTitle(string text)
    {
        ClearControls(PathBar);
        var crumb = CreateClippedLabel(text);
        crumb.AddStyleClass(StyleClass.LabelKeyText);
        PathBar.AddChild(crumb);

        var heading = new Label
        {
            Text = text,
            HorizontalExpand = true,
        };
        heading.AddStyleClass("LabelHeadingBigger");
        PageBody.AddChild(heading);
    }

    private void SetEntryPath(string id)
    {
        ClearControls(PathBar);
        _categories.TryGetValue(_entryCategories.GetValueOrDefault(id, string.Empty), out var category);
        var single = category is { Roots.Count: 1 } ? category.Roots[0] : null;

        var ancestors = new List<string>();
        var visited = new HashSet<string> { id };
        var current = id;
        while (_parents.TryGetValue(current, out var parent) && visited.Add(parent))
        {
            ancestors.Add(parent);
            current = parent;
        }

        ancestors.Reverse();

        if (category != null && id != single)
        {
            var target = category;
            AddCrumb(category.Name, () => OpenCategory(target.Id));
        }

        foreach (var ancestor in ancestors)
        {
            if (ancestor == single)
                continue;

            var target = ancestor;
            AddCrumb(GetEntryName(ancestor), () => OpenEntry(target));
        }

        var name = id == single && category != null ? category.Name : GetEntryName(id);
        var last = CreateClippedLabel(name);
        last.HorizontalExpand = false;
        last.AddStyleClass(StyleClass.LabelKeyText);
        PathBar.AddChild(last);
    }

    private void AddCrumb(string text, Action pressed)
    {
        var button = new ContainerButton
        {
            StyleIdentifier = TreeItem.StyleIdentifierTreeButton,
            ToolTip = text,
        };
        button.AddChild(new Label
        {
            Text = text,
            Margin = new Thickness(4, 0),
            MouseFilter = MouseFilterMode.Ignore,
        });
        button.OnPressed += _ => pressed();
        PathBar.AddChild(button);

        var separator = new Label
        {
            Text = "/",
            MouseFilter = MouseFilterMode.Ignore,
        };
        separator.AddStyleClass(StyleClass.LabelSubText);
        PathBar.AddChild(separator);
    }

    private static RichTextLabel CreateWrappedLabel(string text)
    {
        var label = new RichTextLabel
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        };
        SetText(label, text);
        return label;
    }

    private static Label CreateClippedLabel(string text)
    {
        return new Label
        {
            Text = text,
            ClipText = true,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        };
    }

    private void SetDescription(string text)
    {
        if (_description == null || _description.Disposed)
        {
            _description = CreateText(text);
            PageBody.AddChild(_description);
        }
        else
        {
            SetText(_description, text);
        }
    }

    private static RichTextLabel CreateText(string text)
    {
        var label = new RichTextLabel
        {
            HorizontalExpand = true,
            MouseFilter = MouseFilterMode.Ignore,
        };
        SetText(label, text);
        return label;
    }

    private static void SetText(RichTextLabel label, string text)
    {
        var message = new FormattedMessage();
        message.AddText(text);
        label.SetMessage(message);
    }
}
