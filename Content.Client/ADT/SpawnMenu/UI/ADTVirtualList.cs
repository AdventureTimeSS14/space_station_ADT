using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTVirtualList : ScrollContainer
{
    private readonly ADTSpawnItemContainer _items;
    private readonly Dictionary<int, Control> _controls = new();
    private readonly List<int> _toRemove = new();

    private (int Start, int End) _visible = (0, -1);
    private int _count;
    private bool _grid;
    private Vector2 _listSize = new(0, 36);
    private Vector2 _gridSize = new(76, 76);

    public Func<int, bool, Control>? Factory;

    public bool Grid => _grid;

    public IEnumerable<Control> VisibleControls => _controls.Values;

    public ADTVirtualList()
    {
        HScrollEnabled = false;
        VerticalExpand = true;
        HorizontalExpand = true;

        _items = new ADTSpawnItemContainer();
        AddChild(_items);

        OnScrolled += UpdateVisible;
        OnResized += UpdateVisible;
        _items.OnResized += UpdateVisible;

        ApplyLayout();
    }

    public void SetItemSizes(Vector2 listSize, Vector2 gridSize)
    {
        _listSize = listSize;
        _gridSize = gridSize;
        ApplyLayout();
        Refresh();
    }

    public void SetGrid(bool grid)
    {
        _grid = grid;
        ApplyLayout();
        Refresh();
    }

    public void SetCount(int count, bool resetScroll = true)
    {
        _count = count;
        _items.TotalItemCount = count;

        if (resetScroll)
            SetScrollValue(Vector2.Zero);

        Refresh();
    }

    public void Refresh()
    {
        _items.RemoveAllChildren();
        _controls.Clear();
        _visible = (0, -1);
        UpdateVisible();
    }

    private void ApplyLayout()
    {
        _items.StretchItems = !_grid;
        _items.ItemSize = _grid ? _gridSize : _listSize;
        _items.Columns = 1;
    }

    private void UpdateVisible()
    {
        if (Factory == null)
            return;

        if (_grid)
        {
            var separation = ADTSpawnItemContainer.Separation;
            var columns = Math.Max(1, (int) ((_items.Width + separation) / (_gridSize.X + separation)));
            if (columns != _items.Columns)
            {
                _items.Columns = columns;
                _visible = (0, -1);
            }
        }

        var rowHeight = _items.RowHeight;
        var offset = Math.Max(-_items.Position.Y, 0);
        var firstRow = (int) (offset / rowHeight);
        var lastRow = (int) ((offset + Height) / rowHeight);

        var start = firstRow * _items.Columns;
        var end = Math.Min(_count - 1, (lastRow + 1) * _items.Columns - 1);

        if (start == _visible.Start && end == _visible.End)
            return;

        _visible = (start, end);

        _toRemove.Clear();
        foreach (var (index, control) in _controls)
        {
            if (index >= start && index <= end)
                continue;

            _items.RemoveChild(control);
            _toRemove.Add(index);
        }

        foreach (var index in _toRemove)
        {
            _controls.Remove(index);
        }

        for (var i = start; i <= end; i++)
        {
            if (_controls.ContainsKey(i))
                continue;

            var control = Factory(i, _grid);
            _controls.Add(i, control);
            _items.AddChild(control);
        }
    }
}
