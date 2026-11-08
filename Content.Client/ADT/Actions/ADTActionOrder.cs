using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Actions;

public sealed class ADTActionOrder
{
    public const int MaxEntries = 256;

    private readonly List<EntProtoId> _order = new();
    private readonly Dictionary<EntProtoId, int> _places = new();
    private readonly HashSet<EntProtoId> _removed = new();

    public IReadOnlyList<EntProtoId> Order => _order;

    public IReadOnlyCollection<EntProtoId> Removed => _removed;

    public bool HasPlace(EntProtoId action)
    {
        return _places.ContainsKey(action);
    }

    public bool IsRemoved(EntProtoId action)
    {
        return _removed.Contains(action);
    }

    public void Load(IEnumerable<EntProtoId> order, IEnumerable<EntProtoId> removed)
    {
        _order.Clear();
        _places.Clear();
        _removed.Clear();

        foreach (var action in order)
        {
            if (_order.Count >= MaxEntries)
                break;

            if (_places.TryAdd(action, _order.Count))
                _order.Add(action);
        }

        foreach (var action in removed)
        {
            if (_removed.Count >= MaxEntries)
                break;

            _removed.Add(action);
        }
    }

    public bool Store(IEnumerable<EntProtoId?> hotbar)
    {
        var present = new List<EntProtoId>();
        var seen = new HashSet<EntProtoId>();
        var changed = false;

        foreach (var entry in hotbar)
        {
            if (entry is not { } action || !seen.Add(action))
                continue;

            if (!_places.ContainsKey(action))
            {
                if (_order.Count >= MaxEntries)
                    continue;

                _places[action] = _order.Count;
                _order.Add(action);
                changed = true;
            }

            present.Add(action);
            changed |= _removed.Remove(action);
        }

        var slots = new List<int>(present.Count);
        foreach (var action in present)
        {
            slots.Add(_places[action]);
        }

        slots.Sort();

        for (var i = 0; i < present.Count; i++)
        {
            var action = present[i];
            var slot = slots[i];

            if (_order[slot] == action)
                continue;

            _order[slot] = action;
            _places[action] = slot;
            changed = true;
        }

        return changed;
    }

    public bool SetRemoved(EntProtoId action, bool removed)
    {
        if (!removed)
            return _removed.Remove(action);

        return _removed.Count < MaxEntries && _removed.Add(action);
    }

    public List<T> Arrange<T>(IEnumerable<T> actions, Func<T, EntProtoId?> getKey)
    {
        var known = new List<(int Place, T Action)>();
        var fresh = new List<T>();

        foreach (var action in actions)
        {
            if (getKey(action) is not { } key)
            {
                fresh.Add(action);
                continue;
            }

            if (_removed.Contains(key))
                continue;

            if (_places.TryGetValue(key, out var place))
                known.Add((place, action));
            else
                fresh.Add(action);
        }

        var result = known.OrderBy(entry => entry.Place).Select(entry => entry.Action).ToList();
        result.AddRange(fresh);
        return result;
    }

    public int GetInsertIndex(IReadOnlyList<EntProtoId?> hotbar, EntProtoId action)
    {
        if (!_places.TryGetValue(action, out var place))
            return hotbar.Count;

        for (var i = 0; i < hotbar.Count; i++)
        {
            if (hotbar[i] is not { } other || !_places.TryGetValue(other, out var otherPlace) || otherPlace > place)
                return i;
        }

        return hotbar.Count;
    }
}
