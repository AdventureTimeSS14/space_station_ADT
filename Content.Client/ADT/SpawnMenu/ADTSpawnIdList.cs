using System.Numerics;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.SpawnMenu;

public sealed class ADTSpawnIdList
{
    private readonly List<string> _ids;
    private readonly HashSet<string> _set = new();
    private readonly int _limit;
    private readonly Action _changed;

    public IReadOnlyList<string> Ids => _ids;
    public int Count => _ids.Count;

    public ADTSpawnIdList(List<string> ids, Action changed, int limit = 0)
    {
        _ids = ids;
        _changed = changed;
        _limit = limit;

        for (var i = _ids.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(_ids[i]) || !_set.Add(_ids[i]))
                _ids.RemoveAt(i);
        }
    }

    public bool Contains(string id)
    {
        return _set.Contains(id);
    }

    public int IndexOf(string id)
    {
        return _ids.IndexOf(id);
    }

    public bool Toggle(string id)
    {
        if (_set.Remove(id))
        {
            _ids.Remove(id);
            _changed();
            return false;
        }

        _set.Add(id);
        _ids.Add(id);
        _changed();
        return true;
    }

    public void Push(string id)
    {
        if (_set.Contains(id))
            _ids.Remove(id);
        else
            _set.Add(id);

        _ids.Insert(0, id);

        while (_limit > 0 && _ids.Count > _limit)
        {
            _set.Remove(_ids[_ids.Count - 1]);
            _ids.RemoveAt(_ids.Count - 1);
        }

        _changed();
    }

    public static void LoadSize(Vector2? size, DefaultWindow window)
    {
        if (size is not { } value)
            return;

        window.SetSize = new Vector2(Math.Max(value.X, window.MinSize.X), Math.Max(value.Y, window.MinSize.Y));
    }

    public static void OpenToLeft(DefaultWindow window, Vector2 screenSize)
    {
        if (screenSize.X > 0 && screenSize.Y > 0)
        {
            var size = window.SetSize;
            if (size == Vector2.Zero)
                size = window.Size;

            var max = screenSize * 0.9f;
            var width = Math.Max(window.MinSize.X, Math.Min(size.X, max.X));
            var height = Math.Max(window.MinSize.Y, Math.Min(size.Y, max.Y));
            window.SetSize = new Vector2(width, height);
        }

        window.OpenToLeft();
    }
}
