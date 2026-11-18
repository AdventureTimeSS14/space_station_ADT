using System.Linq;
using Content.Shared.ADT.Guidebook;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Guidebook;

public sealed partial class ADTGuidebookWindow
{
    private void BuildCategories()
    {
        _categories.Clear();
        _entryCategories.Clear();
        _parents.Clear();
        _visibleEntries.Clear();

        if (_requestedRoots != null)
        {
            foreach (var root in _requestedRoots)
            {
                AddVisibleEntries(root.Id);
            }
        }
        else
        {
            foreach (var entry in _entries.Values)
            {
                if (!entry.RuleEntry)
                    _visibleEntries.Add(entry.Id);
            }

            if (_rulesRoot != null)
                AddVisibleEntries(_rulesRoot);
        }

        if (_forcedRoot is { } forced)
            AddVisibleEntries(forced.Id);

        if (_entries.Count == 1)
            _visibleEntries.Add(_entries.Keys.First().Id);

        var owners = new Dictionary<string, string>();
        var prototypes = _prototype.EnumeratePrototypes<ADTGuidebookCategoryPrototype>().OrderBy(p => p.Order);
        foreach (var prototype in prototypes)
        {
            var roots = prototype.Rules
                ? new List<string>()
                : prototype.Entries.Select(id => id.Id).Where(_visibleEntries.Contains).Distinct().ToList();
            if (prototype.Rules && _rulesRoot != null && _visibleEntries.Contains(_rulesRoot))
            {
                roots.Add(_rulesRoot);
            }

            if (roots.Count == 0)
                continue;

            var category = new Category(
                prototype.ID,
                Loc.GetString(prototype.Name),
                Loc.GetString(prototype.Description),
                prototype.Icon,
                roots);
            _categories.Add(category.Id, category);

            foreach (var root in roots)
            {
                owners.TryAdd(root, category.Id);
            }
        }

        foreach (var category in _categories.Values)
        {
            foreach (var root in category.Roots)
            {
                AssignCategory(root, category, owners);
            }
        }

        var remaining = _visibleEntries.Where(id => !_entryCategories.ContainsKey(id)).ToHashSet();
        if (remaining.Count == 0)
            return;

        var children = remaining.SelectMany(id => _entries[id].Children).Select(id => id.Id).ToHashSet();
        var other = new Category(
            "Other",
            Loc.GetString("adt-guidebook-other"),
            Loc.GetString("adt-guidebook-other-description"),
            new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "Librarian"),
            remaining.Where(id => !children.Contains(id)).OrderBy(GetEntryName).ToList());
        _categories.Add(other.Id, other);

        foreach (var root in other.Roots)
        {
            AssignCategory(root, other, owners);
        }

        foreach (var id in remaining.OrderBy(GetEntryName))
        {
            if (_entryCategories.ContainsKey(id))
                continue;

            other.Roots.Add(id);
            AssignCategory(id, other, owners);
        }
    }

    private void AddVisibleEntries(string id)
    {
        var pending = new Stack<string>();
        var visited = new HashSet<string>();
        pending.Push(id);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current) || !_entries.TryGetValue(current, out var entry))
                continue;

            _visibleEntries.Add(current);
            foreach (var child in entry.Children)
            {
                pending.Push(child.Id);
            }
        }
    }

    private void AssignCategory(string id, Category category, Dictionary<string, string> owners)
    {
        if (!_visibleEntries.Contains(id)
            || owners.TryGetValue(id, out var owner) && owner != category.Id
            || _entryCategories.ContainsKey(id))
        {
            return;
        }

        _entryCategories.Add(id, category.Id);

        foreach (var child in _entries[id].Children)
        {
            AssignCategory(child.Id, category, owners);
            if (_entryCategories.GetValueOrDefault(child.Id) == category.Id && !category.Roots.Contains(child.Id))
                _parents.TryAdd(child.Id, id);
        }
    }

    private string GetEntryName(string id)
    {
        return _entries.TryGetValue(id, out var entry) ? Loc.GetString(entry.Name) : id;
    }

    private string GetEntryPath(string id)
    {
        var names = new List<string>();
        var visited = new HashSet<string>();
        var current = id;
        _categories.TryGetValue(_entryCategories.GetValueOrDefault(id, string.Empty), out var category);
        while (visited.Add(current))
        {
            if (current == id || category == null || !category.Roots.Contains(current))
                names.Add(GetEntryName(current));
            if (!_parents.TryGetValue(current, out var parent))
                break;

            current = parent;
        }

        names.Reverse();
        if (category != null && (names.Count == 0 || names[0] != category.Name))
            names.Insert(0, category.Name);

        return string.Join(" / ", names);
    }

    private sealed record Category(string Id, string Name, string Description, SpriteSpecifier Icon, List<string> Roots);
}
