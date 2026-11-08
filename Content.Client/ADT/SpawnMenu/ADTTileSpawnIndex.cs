using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;

namespace Content.Client.ADT.SpawnMenu;

public sealed class ADTTileSpawnEntry
{
    public readonly ContentTileDefinition Definition;
    public readonly string Name;
    public readonly string SearchKey;
    public ADTSpawnCategoryNode Category = default!;

    private Texture? _texture;
    private bool _textureLoaded;

    public ADTTileSpawnEntry(ContentTileDefinition definition, string name)
    {
        Definition = definition;
        Name = name;
        SearchKey = (definition.ID + "\n" + name).ToLowerInvariant();
    }

    public Texture? GetTexture(IResourceCache resources)
    {
        if (_textureLoaded)
            return _texture;

        _textureLoaded = true;
        var path = Definition.Sprite?.ToString();
        if (path != null)
            _texture = resources.GetResource<TextureResource>(path).Texture;

        return _texture;
    }

    public Texture? GetVariant(IResourceCache resources, int variant)
    {
        var texture = GetTexture(resources);
        if (texture == null)
            return null;

        var variants = Math.Max(1, (int) Definition.Variants);
        var width = texture.Width / variants;
        return new AtlasTexture(texture, UIBox2.FromDimensions(width * variant, 0, width, texture.Height));
    }
}

public sealed class ADTTileSpawnIndex
{
    [Dependency] private ITileDefinitionManager _tiles = default!;

    private const int MinGroupSize = 3;

    public readonly List<ADTTileSpawnEntry> Entries = new();
    public readonly List<ADTSpawnCategoryNode> Roots = new();
    public readonly List<ADTSpawnCategoryNode> Nodes = new();

    private readonly Dictionary<string, ADTSpawnCategoryNode> _nodes = new();

    public ADTSpawnCategoryNode All { get; private set; } = default!;
    public ADTSpawnCategoryNode Favorites { get; private set; } = default!;
    public ADTSpawnCategoryNode Recent { get; private set; } = default!;

    public ADTTileSpawnIndex()
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

        All = AddNode("All", Loc.GetString("adt-spawn-menu-category-all"), null);
        Favorites = AddNode("Favorites", Loc.GetString("adt-spawn-menu-category-favorites"), null);
        Recent = AddNode("Recent", Loc.GetString("adt-spawn-menu-category-recent"), null);
        var floors = AddNode("Floors", Loc.GetString("adt-spawn-menu-tile-category-floors"), null);
        var subfloors = AddNode("Subfloors", Loc.GetString("adt-spawn-menu-tile-category-subfloors"), null);
        var planet = AddNode("Planet", Loc.GetString("adt-spawn-menu-tile-category-planet"), null);
        var other = AddNode("Other", Loc.GetString("adt-spawn-menu-category-other"), null);

        var floorGroups = new Dictionary<string, List<ADTTileSpawnEntry>>();

        foreach (var definition in _tiles)
        {
            if (definition.EditorHidden || definition is not ContentTileDefinition tile)
                continue;

            var entry = new ADTTileSpawnEntry(tile, Loc.GetString(tile.Name));
            Entries.Add(entry);

            if (tile.IsSubFloor)
            {
                entry.Category = subfloors;
            }
            else if (tile.ID == ContentTileDefinition.SpaceID)
            {
                entry.Category = other;
            }
            else if (tile.Weather)
            {
                entry.Category = planet;
            }
            else
            {
                var key = GetGroupKey(tile.Name);
                if (!floorGroups.TryGetValue(key, out var group))
                {
                    group = new List<ADTTileSpawnEntry>();
                    floorGroups.Add(key, group);
                }

                group.Add(entry);
            }
        }

        var otherFloors = AddNode("Floors/Other", Loc.GetString("adt-spawn-menu-tile-category-floors-other"), floors);
        foreach (var (key, group) in floorGroups)
        {
            if (group.Count < MinGroupSize)
            {
                foreach (var entry in group)
                {
                    entry.Category = otherFloors;
                }

                continue;
            }

            var node = AddNode("Floors/" + key, GetGroupName(group), floors);
            foreach (var entry in group)
            {
                entry.Category = node;
            }
        }

        floors.Children.Sort((a, b) =>
        {
            if (a == otherFloors || b == otherFloors)
                return a == otherFloors ? 1 : -1;

            return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
        });

        Entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCulture));
    }

    private ADTSpawnCategoryNode AddNode(string id, string name, ADTSpawnCategoryNode? parent)
    {
        var node = new ADTSpawnCategoryNode(id, name, parent, false, 0);
        _nodes[id] = node;
        Nodes.Add(node);

        if (parent == null)
            Roots.Add(node);
        else
            parent.Children.Add(node);

        return node;
    }

    private static string GetGroupKey(string locId)
    {
        var parts = locId.Split('-');
        if (parts.Length >= 2 && parts[0] == "tiles")
            return parts[1];

        return locId;
    }

    private static string GetGroupName(List<ADTTileSpawnEntry> group)
    {
        var shortest = group[0];
        foreach (var entry in group)
        {
            if (entry.Definition.Name.Length < shortest.Definition.Name.Length)
                shortest = entry;
        }

        var name = shortest.Name;
        if (name.Length == 0)
            return shortest.Definition.ID;

        return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
    }
}
