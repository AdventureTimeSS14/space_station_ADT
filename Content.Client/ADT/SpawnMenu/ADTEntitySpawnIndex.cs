using System.Linq;
using Content.Shared.ADT.SpawnMenu;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.SpawnMenu;

public sealed class ADTSpawnCategoryNode
{
    public readonly string Id;
    public readonly string Name;
    public readonly ADTSpawnCategoryNode? Parent;
    public readonly bool Service;
    public readonly int Order;
    public readonly List<ADTSpawnCategoryNode> Children = new();
    public int Count;

    public ADTSpawnCategoryNode(string id, string name, ADTSpawnCategoryNode? parent, bool service, int order)
    {
        Id = id;
        Name = name;
        Parent = parent;
        Service = service;
        Order = order;
    }

    public bool Contains(ADTSpawnCategoryNode? node)
    {
        while (node != null)
        {
            if (node == this)
                return true;

            node = node.Parent;
        }

        return false;
    }
}

public sealed class ADTEntitySpawnEntry
{
    public readonly EntityPrototype Prototype;
    public readonly string SearchKey;
    public readonly string IdLower;
    public readonly string NameLower;
    public readonly List<(string Raw, string Normalized)> SuffixTags;
    public ADTSpawnCategoryNode? Category;
    public ADTSpawnCategoryNode? ServiceCategory;
    public HashSet<string>? AncestorsLower;
    public bool Untranslated;

    public ADTEntitySpawnEntry(EntityPrototype prototype, List<(string Raw, string Normalized)> suffixTags)
    {
        Prototype = prototype;
        SuffixTags = suffixTags;
        IdLower = prototype.ID.ToLowerInvariant();
        NameLower = prototype.Name.ToLowerInvariant();
        SearchKey = (prototype.ID + "\n" + prototype.Name + "\n" + prototype.EditorSuffix).ToLowerInvariant();
    }

    public bool HasTag(string normalized)
    {
        foreach (var tag in SuffixTags)
        {
            if (tag.Normalized == normalized)
                return true;
        }

        return false;
    }
}

public sealed class ADTSpawnListItem
{
    public readonly ADTEntitySpawnEntry Representative;
    public readonly List<ADTEntitySpawnEntry> Variants;

    public ADTSpawnListItem(ADTEntitySpawnEntry representative, List<ADTEntitySpawnEntry> variants)
    {
        Representative = representative;
        Variants = variants;
    }

    public bool Contains(EntityPrototype? prototype)
    {
        if (prototype == null)
            return false;

        foreach (var variant in Variants)
        {
            if (variant.Prototype == prototype)
                return true;
        }

        return false;
    }
}

public sealed class ADTEntitySpawnIndex
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private ILocalizationManager _loc = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IResourceManager _resources = default!;

    private const string AllId = "All";
    private const string OtherId = "Other";
    private const string FavoritesId = "Favorites";
    private const string RecentId = "Recent";
    private const string UntranslatedId = "Untranslated";
    private const string RussianLanguage = "ru";
    private const string EntitiesFolder = "/entities/";
    private const string StructuralDamage = "Structural";

    private static readonly ResPath PrototypesRoot = new("/Prototypes");
    private static readonly char[] QuoteChars = { '"', '\'' };

    private sealed class Rule
    {
        public ADTSpawnCategoryPrototype Prototype = default!;
        public ADTSpawnCategoryNode Node = default!;
        public HashSet<string> SuffixTags = default!;
        public List<string> Paths = default!;
        public List<string> ExcludePaths = default!;
        public SlotFlags Slots;
    }

    private sealed class Context
    {
        public EntityPrototype Prototype = default!;
        public string Path = string.Empty;
        public List<(string Raw, string Normalized)> SuffixTags = default!;
        public HashSet<string>? Ancestors;
        public float? MeleeDamage;
    }

    private readonly List<Rule> _rules = new();
    private readonly Dictionary<string, ADTSpawnCategoryNode> _nodes = new();
    private Dictionary<string, string>? _paths;
    private readonly ISawmill _sawmill;

    public readonly List<ADTEntitySpawnEntry> Entries = new();
    public readonly List<ADTSpawnCategoryNode> Roots = new();
    public readonly List<ADTSpawnCategoryNode> Nodes = new();

    public ADTSpawnCategoryNode All { get; private set; } = default!;
    public ADTSpawnCategoryNode Other { get; private set; } = default!;
    public ADTSpawnCategoryNode Favorites { get; private set; } = default!;
    public ADTSpawnCategoryNode Recent { get; private set; } = default!;
    public ADTSpawnCategoryNode? Untranslated { get; private set; }

    public ADTEntitySpawnIndex()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("adt.spawnmenu");
    }

    public ADTSpawnCategoryNode? GetNode(string id)
    {
        return _nodes.GetValueOrDefault(id);
    }

    public void InvalidatePaths()
    {
        _paths = null;
    }

    public void Rebuild()
    {
        Entries.Clear();
        Roots.Clear();
        Nodes.Clear();
        _nodes.Clear();
        _rules.Clear();

        EnsurePaths();

        All = AddNode(AllId, Loc.GetString("adt-spawn-menu-category-all"), null, false, int.MinValue);
        Favorites = AddNode(FavoritesId, Loc.GetString("adt-spawn-menu-category-favorites"), null, false, int.MinValue + 1);
        Recent = AddNode(RecentId, Loc.GetString("adt-spawn-menu-category-recent"), null, false, int.MinValue + 2);

        var checkTranslation = _loc.DefaultCulture?.TwoLetterISOLanguageName == RussianLanguage;
        Untranslated = checkTranslation
            ? AddNode(UntranslatedId, Loc.GetString("adt-spawn-menu-category-untranslated"), null, false, int.MaxValue - 1)
            : null;
        Other = AddNode(OtherId, Loc.GetString("adt-spawn-menu-category-other"), null, false, int.MaxValue);

        foreach (var category in _prototypes.EnumeratePrototypes<ADTSpawnCategoryPrototype>())
        {
            if (category.RuleFor == null)
                GetOrCreateNode(category);
        }

        BuildRules();

        var context = new Context();
        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract || prototype.HideSpawnMenu)
                continue;

            var entry = new ADTEntitySpawnEntry(prototype, SplitSuffix(prototype.EditorSuffix))
            {
                Untranslated = checkTranslation && !HasCyrillic(prototype.Name),
            };

            context.Prototype = prototype;
            context.Path = _paths!.GetValueOrDefault(prototype.ID, string.Empty);
            context.SuffixTags = entry.SuffixTags;
            context.Ancestors = null;
            context.MeleeDamage = null;

            Classify(entry, context);
            Entries.Add(entry);
        }

        Entries.Sort((a, b) =>
        {
            var names = string.Compare(a.Prototype.Name, b.Prototype.Name, StringComparison.Ordinal);
            if (names != 0)
                return names;

            return string.Compare(a.Prototype.EditorSuffix, b.Prototype.EditorSuffix, StringComparison.Ordinal);
        });

        SortChildren(Roots);
    }

    private void EnsurePaths()
    {
        if (_paths != null)
            return;

        _paths = new Dictionary<string, string>();

        foreach (var file in _resources.ContentFindFiles(PrototypesRoot))
        {
            if (file.Extension != "yml" || file.Filename.StartsWith("."))
                continue;

            var path = NormalizeFilePath(file.ToString());
            using var reader = _resources.ContentFileReadText(file);

            string? kind = null;
            string? id = null;

            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith("- "))
                {
                    StorePath(kind, id, path);
                    kind = null;
                    id = null;
                    ParseKey(line.Substring(2), ref kind, ref id);
                    continue;
                }

                if (line.StartsWith("  ") && !line.StartsWith("   "))
                    ParseKey(line.Substring(2), ref kind, ref id);
            }

            StorePath(kind, id, path);
        }
    }

    private void StorePath(string? kind, string? id, string path)
    {
        if (kind == "entity" && !string.IsNullOrEmpty(id))
            _paths![id] = path;
    }

    private static void ParseKey(string line, ref string? kind, ref string? id)
    {
        if (line.StartsWith("type:"))
        {
            kind = CleanValue(line.Substring(5));
            return;
        }

        if (line.StartsWith("id:"))
            id = CleanValue(line.Substring(3));
    }

    private static string CleanValue(string value)
    {
        var comment = value.IndexOf('#');
        if (comment >= 0)
            value = value.Substring(0, comment);

        return value.Trim().Trim(QuoteChars);
    }

    private static string NormalizeFilePath(string path)
    {
        path = path.ToLowerInvariant();
        if (path.StartsWith("/prototypes"))
            path = path.Substring("/prototypes".Length);

        var entities = path.LastIndexOf(EntitiesFolder, StringComparison.Ordinal);
        if (entities >= 0)
            path = path.Substring(entities + EntitiesFolder.Length - 1);

        return path;
    }

    private static string NormalizeRulePath(string path)
    {
        return "/" + path.TrimStart('/').ToLowerInvariant();
    }

    private ADTSpawnCategoryNode AddNode(string id, string name, ADTSpawnCategoryNode? parent, bool service, int order)
    {
        var node = new ADTSpawnCategoryNode(id, name, parent, service, order);
        _nodes[id] = node;
        Nodes.Add(node);

        if (parent == null)
            Roots.Add(node);
        else
            parent.Children.Add(node);

        return node;
    }

    private ADTSpawnCategoryNode GetOrCreateNode(ADTSpawnCategoryPrototype category)
    {
        if (_nodes.TryGetValue(category.ID, out var existing))
            return existing;

        ADTSpawnCategoryNode? parent = null;
        if (category.ParentCategory != null && _prototypes.TryIndex(category.ParentCategory.Value, out var parentCategory))
            parent = GetOrCreateNode(parentCategory);

        var service = category.Service || parent is { Service: true };
        var name = category.Name != null ? Loc.GetString(category.Name.Value) : category.ID;
        return AddNode(category.ID, name, parent, service, category.Order);
    }

    private void BuildRules()
    {
        foreach (var category in _prototypes.EnumeratePrototypes<ADTSpawnCategoryPrototype>())
        {
            if (!HasCriteria(category))
                continue;

            var target = category.RuleFor?.Id ?? category.ID;
            if (!_nodes.TryGetValue(target, out var node))
            {
                _sawmill.Warning($"Spawn category rule {category.ID} points to unknown category {target}");
                continue;
            }

            ValidateComponents(category, category.Components);
            ValidateComponents(category, category.AnyComponents);
            ValidateComponents(category, category.ExcludeComponents);

            var slots = SlotFlags.NONE;
            foreach (var slot in category.ClothingSlots)
            {
                slots |= slot;
            }

            _rules.Add(new Rule
            {
                Prototype = category,
                Node = node,
                SuffixTags = category.SuffixTags.Select(Normalize).ToHashSet(),
                Paths = category.Paths.Select(NormalizeRulePath).ToList(),
                ExcludePaths = category.ExcludePaths.Select(NormalizeRulePath).ToList(),
                Slots = slots,
            });
        }

        _rules.Sort((a, b) =>
        {
            var priority = b.Prototype.Priority.CompareTo(a.Prototype.Priority);
            if (priority != 0)
                return priority;

            return string.Compare(a.Prototype.ID, b.Prototype.ID, StringComparison.Ordinal);
        });
    }

    private static bool HasCriteria(ADTSpawnCategoryPrototype category)
    {
        return category.Paths.Count > 0
               || category.Components.Count > 0
               || category.AnyComponents.Count > 0
               || category.ClothingSlots.Count > 0
               || category.Tags.Count > 0
               || category.Ancestors.Count > 0
               || category.IdPrefixes.Count > 0
               || category.SuffixTags.Count > 0
               || category.EntityCategories.Count > 0
               || category.MinMeleeDamage != null;
    }

    private void ValidateComponents(ADTSpawnCategoryPrototype category, List<string> components)
    {
        foreach (var name in components)
        {
            if (_factory.GetComponentAvailability(name) == ComponentAvailability.Available)
                continue;

            _sawmill.Warning($"Spawn category {category.ID} uses component {name}, which is not available on the client");
        }
    }

    private void Classify(ADTEntitySpawnEntry entry, Context context)
    {
        foreach (var rule in _rules)
        {
            var service = rule.Node.Service;
            if (service && entry.ServiceCategory != null)
                continue;

            if (!service && entry.Category != null)
                continue;

            if (!Matches(rule, context))
                continue;

            var node = rule.Node;
            if (rule.Prototype.SplitBySuffix)
                node = GetSplitNode(rule, context.SuffixTags);

            if (service)
                entry.ServiceCategory = node;
            else
                entry.Category = node;

            if (entry.Category != null && entry.ServiceCategory != null)
                break;
        }
    }

    private bool Matches(Rule rule, Context context)
    {
        var category = rule.Prototype;
        var prototype = context.Prototype;
        var components = prototype.Components;

        if (rule.Paths.Count > 0 && !rule.Paths.Any(p => context.Path.Contains(p, StringComparison.Ordinal)))
            return false;

        if (rule.ExcludePaths.Any(p => context.Path.Contains(p, StringComparison.Ordinal)))
            return false;

        if (rule.SuffixTags.Count > 0 && !context.SuffixTags.Any(t => rule.SuffixTags.Contains(t.Normalized)))
            return false;

        foreach (var name in category.Components)
        {
            if (!components.ContainsKey(name))
                return false;
        }

        if (category.AnyComponents.Count > 0 && !category.AnyComponents.Any(components.ContainsKey))
            return false;

        if (category.ExcludeComponents.Any(components.ContainsKey))
            return false;

        if (category.IdPrefixes.Count > 0 && !category.IdPrefixes.Any(p => prototype.ID.StartsWith(p, StringComparison.Ordinal)))
            return false;

        if (category.EntityCategories.Count > 0 && !prototype.Categories.Any(c => category.EntityCategories.Contains(c.ID)))
            return false;

        if (rule.Slots != SlotFlags.NONE)
        {
            if (!prototype.TryComp<ClothingComponent>(out var clothing, _factory))
                return false;

            if ((clothing.Slots & rule.Slots) == SlotFlags.NONE)
                return false;
        }

        if (category.Tags.Count > 0)
        {
            if (!prototype.TryComp<TagComponent>(out var tags, _factory))
                return false;

            if (!category.Tags.Any(tags.Tags.Contains))
                return false;
        }

        if (category.Ancestors.Count > 0)
        {
            context.Ancestors ??= GetAncestors(prototype);
            if (!category.Ancestors.Any(context.Ancestors.Contains))
                return false;
        }

        if (category.MinMeleeDamage != null)
        {
            context.MeleeDamage ??= GetMeleeDamage(prototype);
            if (context.MeleeDamage < category.MinMeleeDamage)
                return false;
        }

        return true;
    }

    public HashSet<string> GetAncestorsLower(ADTEntitySpawnEntry entry)
    {
        if (entry.AncestorsLower != null)
            return entry.AncestorsLower;

        entry.AncestorsLower = new HashSet<string>();
        foreach (var (id, _) in _prototypes.EnumerateAllParents<EntityPrototype>(entry.Prototype.ID))
        {
            entry.AncestorsLower.Add(id.ToLowerInvariant());
        }

        return entry.AncestorsLower;
    }

    private HashSet<string> GetAncestors(EntityPrototype prototype)
    {
        var result = new HashSet<string>();
        foreach (var (id, _) in _prototypes.EnumerateAllParents<EntityPrototype>(prototype.ID))
        {
            result.Add(id);
        }

        return result;
    }

    private float GetMeleeDamage(EntityPrototype prototype)
    {
        if (!prototype.TryComp<MeleeWeaponComponent>(out var melee, _factory))
            return 0f;

        var total = 0f;
        foreach (var (type, value) in melee.Damage.DamageDict)
        {
            if (type.Id != StructuralDamage)
                total += value.Float();
        }

        return total;
    }

    private ADTSpawnCategoryNode GetSplitNode(Rule rule, List<(string Raw, string Normalized)> suffixTags)
    {
        var afterMatch = false;
        foreach (var (raw, normalized) in suffixTags)
        {
            if (rule.SuffixTags.Contains(normalized))
            {
                afterMatch = true;
                continue;
            }

            if (!afterMatch)
                continue;

            var id = rule.Node.Id + "/" + normalized;
            if (_nodes.TryGetValue(id, out var existing))
                return existing;

            return AddNode(id, raw, rule.Node, rule.Node.Service, 0);
        }

        return rule.Node;
    }

    private static List<(string Raw, string Normalized)> SplitSuffix(string? suffix)
    {
        var result = new List<(string Raw, string Normalized)>();
        if (string.IsNullOrWhiteSpace(suffix))
            return result;

        foreach (var part in suffix.Split(','))
        {
            var raw = part.Trim();
            if (raw.Length == 0)
                continue;

            result.Add((raw, Normalize(raw)));
        }

        return result;
    }

    private static bool HasCyrillic(string text)
    {
        foreach (var ch in text)
        {
            if (ch is >= 'А' and <= 'я' or 'Ё' or 'ё')
                return true;
        }

        return false;
    }

    public static string Normalize(string tag)
    {
        return tag.ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty);
    }

    private static void SortChildren(List<ADTSpawnCategoryNode> nodes)
    {
        nodes.Sort((a, b) =>
        {
            if (a.Service != b.Service)
                return a.Service ? 1 : -1;

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
