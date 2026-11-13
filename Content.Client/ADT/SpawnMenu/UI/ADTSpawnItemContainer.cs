using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public interface IADTSpawnItem
{
    int Index { get; }
}

public sealed class ADTSpawnItemContainer : Container
{
    public const float Separation = 2;

    private int _totalItemCount;
    private int _columns = 1;
    private Vector2 _itemSize = new(32, 32);
    private bool _stretchItems = true;

    public int TotalItemCount
    {
        get => _totalItemCount;
        set
        {
            _totalItemCount = value;
            InvalidateMeasure();
        }
    }

    public int Columns
    {
        get => _columns;
        set
        {
            _columns = Math.Max(1, value);
            InvalidateMeasure();
        }
    }

    public Vector2 ItemSize
    {
        get => _itemSize;
        set
        {
            _itemSize = value;
            InvalidateMeasure();
        }
    }

    public bool StretchItems
    {
        get => _stretchItems;
        set
        {
            _stretchItems = value;
            InvalidateMeasure();
        }
    }

    public float RowHeight => _itemSize.Y + Separation;

    public int RowCount => (_totalItemCount + _columns - 1) / _columns;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var childSize = new Vector2(_stretchItems ? availableSize.X : _itemSize.X, _itemSize.Y);
        foreach (var child in Children)
        {
            child.Measure(childSize);
        }

        var height = Math.Max(0, RowCount * RowHeight - Separation);
        return new Vector2(_itemSize.X, height);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var width = _stretchItems ? finalSize.X : _itemSize.X;
        foreach (var child in Children)
        {
            if (child is not IADTSpawnItem item)
                continue;

            var row = item.Index / _columns;
            var column = item.Index % _columns;
            child.Arrange(UIBox2.FromDimensions(column * (width + Separation), row * RowHeight, width, _itemSize.Y));
        }

        return finalSize;
    }
}
