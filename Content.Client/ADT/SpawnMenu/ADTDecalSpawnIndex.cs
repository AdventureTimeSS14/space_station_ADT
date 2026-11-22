using Content.Shared.Decals;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.SpawnMenu;

public sealed class ADTDecalSpawnEntry
{
    public readonly DecalPrototype Prototype;
    public readonly string SearchKey;
    public readonly string GroupKey;
    public readonly string Direction;
    public ADTSpawnCategoryNode Category = default!;

    public string Id => Prototype.ID;

    public ADTDecalSpawnEntry(DecalPrototype prototype, string groupKey, string direction)
    {
        Prototype = prototype;
        GroupKey = groupKey;
        Direction = direction;
        SearchKey = prototype.ID.ToLowerInvariant();
    }
}

public sealed class ADTDecalSpawnItem
{
    public readonly ADTDecalSpawnEntry Representative;
    public readonly List<ADTDecalSpawnEntry> Variants;
    public readonly string Name;

    public ADTDecalSpawnItem(ADTDecalSpawnEntry representative, List<ADTDecalSpawnEntry> variants, string name)
    {
        Representative = representative;
        Variants = variants;
        Name = name;
    }

    public bool Contains(ADTDecalSpawnEntry? entry)
    {
        return entry != null && Variants.Contains(entry);
    }
}

public sealed class ADTDecalSpawnIndex
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private const string TagLocPrefix = "adt-spawn-menu-decal-tag-";

    private static readonly string[] Directions = { "NE", "NW", "SE", "SW", "N", "E", "S", "W" };

    public readonly List<ADTDecalSpawnEntry> Entries = new();
    public readonly List<ADTSpawnCategoryNode> Roots = new();
    public readonly List<ADTSpawnCategoryNode> Nodes = new();

    private readonly Dictionary<string, ADTSpawnCategoryNode> _nodes = new();

    public ADTSpawnCategoryNode All { get; private set; } = default!;
    public ADTSpawnCategoryNode Favorites { get; private set; } = default!;
    public ADTSpawnCategoryNode Recent { get; private set; } = default!;
    public ADTSpawnCategoryNode Other { get; private set; } = default!;

    public ADTDecalSpawnIndex()
    {
        IoCManager.InjectDependencies(this);
    }

    public ADTSpawnCategoryNode? GetNode(string id)
    {
        return _nodes.GetValueOrDefault(id);
    }

    public void Rebuild()
    {
        Entries.Clear();
        Roots.Clear();
        Nodes.Clear();
        _nodes.Clear();

        All = AddNode("All", Loc.GetString("adt-spawn-menu-category-all"), null, int.MinValue);
        Favorites = AddNode("Favorites", Loc.GetString("adt-spawn-menu-category-favorites"), null, int.MinValue + 1);
        Recent = AddNode("Recent", Loc.GetString("adt-spawn-menu-category-recent"), null, int.MinValue + 2);
        Other = AddNode("Other", Loc.GetString("adt-spawn-menu-category-other"), null, int.MaxValue);

        var ids = new HashSet<string>();
        foreach (var prototype in _prototypes.EnumeratePrototypes<DecalPrototype>())
        {
            if (prototype.ShowMenu)
                ids.Add(prototype.ID);
        }

        foreach (var prototype in _prototypes.EnumeratePrototypes<DecalPrototype>())
        {
            if (!prototype.ShowMenu)
                continue;

            var (groupKey, direction) = SplitDirection(prototype.ID, ids);
            var entry = new ADTDecalSpawnEntry(prototype, groupKey, direction)
            {
                Category = GetCategory(prototype.Tags),
            };
            Entries.Add(entry);
        }

        Entries.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));
        SortChildren(Roots);
    }

    private static (string GroupKey, string Direction) SplitDirection(string id, HashSet<string> ids)
    {
        foreach (var direction in Directions)
        {
            if (id.Length <= direction.Length || !id.EndsWith(direction, StringComparison.Ordinal))
                continue;

            var baseId = id.Substring(0, id.Length - direction.Length);
            if (!char.IsLower(baseId[baseId.Length - 1]) && !char.IsDigit(baseId[baseId.Length - 1]))
                continue;

            foreach (var other in Directions)
            {
                if (other != direction && ids.Contains(baseId + other))
                    return (baseId, direction);
            }
        }

        return (id, string.Empty);
    }

    private ADTSpawnCategoryNode GetCategory(List<string> tags)
    {
        if (tags.Count == 0)
            return Other;

        ADTSpawnCategoryNode? node = null;
        var id = string.Empty;

        foreach (var tag in tags)
        {
            id = id.Length == 0 ? tag : id + "/" + tag;
            if (!_nodes.TryGetValue(id, out var existing))
                existing = AddNode(id, GetTagName(tag), node, 0);

            node = existing;
        }

        return node!;
    }

    private static string GetTagName(string tag)
    {
        if (Loc.TryGetString(TagLocPrefix + tag, out var name))
            return name;

        return tag;
    }

    private ADTSpawnCategoryNode AddNode(string id, string name, ADTSpawnCategoryNode? parent, int order)
    {
        var node = new ADTSpawnCategoryNode("decal:" + id, name, parent, false, order);
        _nodes[node.Id] = node;
        _nodes[id] = node;
        Nodes.Add(node);

        if (parent == null)
            Roots.Add(node);
        else
            parent.Children.Add(node);

        return node;
    }

    private static void SortChildren(List<ADTSpawnCategoryNode> nodes)
    {
        nodes.Sort((a, b) =>
        {
            var order = a.Order.CompareTo(b.Order);
            if (order != 0)
                return order;

            return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
        });

        foreach (var node in nodes)
        {
            SortChildren(node.Children);
        }
    }
}
