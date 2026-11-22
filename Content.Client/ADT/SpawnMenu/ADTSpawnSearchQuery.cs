namespace Content.Client.ADT.SpawnMenu;

public sealed class ADTSpawnSearchQuery
{
    private readonly List<string> _words = new();
    private readonly List<string> _ids = new();
    private readonly List<string> _suffixes = new();
    private readonly List<string> _components = new();
    private readonly List<string> _ancestors = new();
    private readonly List<string> _tags = new();

    public bool IsEmpty => _words.Count == 0
                           && _ids.Count == 0
                           && _suffixes.Count == 0
                           && _components.Count == 0
                           && _ancestors.Count == 0
                           && _tags.Count == 0;

    public string? RankToken { get; private set; }

    public static ADTSpawnSearchQuery Parse(string text)
    {
        var query = new ADTSpawnSearchQuery();

        foreach (var raw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var token = raw.ToLowerInvariant();

            if (token.StartsWith("id:"))
                AddValue(query._ids, token.Substring(3));
            else if (token.StartsWith("s:"))
                AddValue(query._suffixes, ADTEntitySpawnIndex.Normalize(token.Substring(2)));
            else if (token.StartsWith("c:"))
                AddValue(query._components, token.Substring(2));
            else if (token.StartsWith("p:"))
                AddValue(query._ancestors, token.Substring(2));
            else if (token.StartsWith("#"))
                AddValue(query._tags, ADTEntitySpawnIndex.Normalize(token.Substring(1)));
            else
                query._words.Add(token);
        }

        if (query._ids.Count > 0)
            query.RankToken = query._ids[0];
        else if (query._words.Count > 0)
            query.RankToken = query._words[0];

        return query;
    }

    private static void AddValue(List<string> list, string value)
    {
        if (value.Length > 0)
            list.Add(value);
    }

    public bool Matches(ADTEntitySpawnEntry entry, ADTEntitySpawnIndex index)
    {
        foreach (var word in _words)
        {
            if (!entry.SearchKey.Contains(word, StringComparison.Ordinal))
                return false;
        }

        foreach (var id in _ids)
        {
            if (!entry.IdLower.Contains(id, StringComparison.Ordinal))
                return false;
        }

        foreach (var suffix in _suffixes)
        {
            if (!HasTagContaining(entry, suffix))
                return false;
        }

        foreach (var tag in _tags)
        {
            if (!entry.HasTag(tag))
                return false;
        }

        foreach (var component in _components)
        {
            if (!HasComponent(entry, component))
                return false;
        }

        if (_ancestors.Count > 0)
        {
            var ancestors = index.GetAncestorsLower(entry);
            foreach (var ancestor in _ancestors)
            {
                if (!ancestors.Contains(ancestor))
                    return false;
            }
        }

        return true;
    }

    public int Rank(ADTEntitySpawnEntry entry)
    {
        if (RankToken == null)
            return 0;

        if (entry.IdLower == RankToken || entry.NameLower == RankToken)
            return 0;

        if (entry.NameLower.StartsWith(RankToken, StringComparison.Ordinal))
            return 1;

        if (entry.IdLower.StartsWith(RankToken, StringComparison.Ordinal))
            return 2;

        return 3;
    }

    private static bool HasTagContaining(ADTEntitySpawnEntry entry, string value)
    {
        foreach (var tag in entry.SuffixTags)
        {
            if (tag.Normalized.Contains(value, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool HasComponent(ADTEntitySpawnEntry entry, string component)
    {
        foreach (var name in entry.Prototype.Components.Keys)
        {
            if (string.Equals(name, component, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
