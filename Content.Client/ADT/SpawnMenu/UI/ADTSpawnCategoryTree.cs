using Content.Client.UserInterface.Controls.FancyTree;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTSpawnCategoryTree
{
    private readonly FancyTree _tree;
    private readonly Dictionary<ADTSpawnCategoryNode, TreeItem> _items = new();
    private bool _updating;

    public event Action<ADTSpawnCategoryNode?>? OnSelected;

    public ADTSpawnCategoryTree(FancyTree tree)
    {
        _tree = tree;
        _tree.OnSelectedItemChanged += OnTreeSelectionChanged;
    }

    public void SetCategories(IReadOnlyList<ADTSpawnCategoryNode> roots, ADTSpawnCategoryNode? selected)
    {
        _updating = true;

        _tree.Clear();
        _items.Clear();

        foreach (var root in roots)
        {
            AddCategory(root, null);
        }

        if (selected != null && _items.TryGetValue(selected, out var item))
        {
            _tree.SetSelectedIndex(item.Index);
            _tree.ExpandParentEntries(item.Index);
        }

        _updating = false;
    }

    public void UpdateCounts(bool hideEmpty)
    {
        var selected = _tree.SelectedItem;

        foreach (var (node, item) in _items)
        {
            item.Label.Text = Loc.GetString("adt-spawn-menu-category", ("name", node.Name), ("count", node.Count));
            item.Visible = !hideEmpty || node.Count > 0 || item == selected;
        }
    }

    private void AddCategory(ADTSpawnCategoryNode node, TreeItem? parent)
    {
        var item = _tree.AddItem(parent);
        item.Metadata = node;
        item.Label.Text = node.Name;

        if (node.Service)
            item.Label.FontColorOverride = Color.FromHex("#E8B05A");

        _items[node] = item;

        foreach (var child in node.Children)
        {
            AddCategory(child, item);
        }
    }

    private void OnTreeSelectionChanged(TreeItem? item)
    {
        if (_updating)
            return;

        OnSelected?.Invoke(item?.Metadata as ADTSpawnCategoryNode);
    }
}
