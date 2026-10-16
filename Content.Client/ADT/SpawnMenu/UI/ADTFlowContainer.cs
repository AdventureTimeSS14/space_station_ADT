using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTFlowContainer : Container
{
    public float HSeparation = 3;
    public float VSeparation = 3;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var x = 0f;
        var y = 0f;
        var rowHeight = 0f;
        var width = 0f;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.Measure(availableSize);
            var size = child.DesiredSize;

            if (x > 0 && x + size.X > availableSize.X)
            {
                y += rowHeight + VSeparation;
                x = 0;
                rowHeight = 0;
            }

            x += size.X + HSeparation;
            rowHeight = Math.Max(rowHeight, size.Y);
            width = Math.Max(width, x - HSeparation);
        }

        return new Vector2(Math.Min(width, availableSize.X), y + rowHeight);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var x = 0f;
        var y = 0f;
        var rowHeight = 0f;

        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            var size = child.DesiredSize;

            if (x > 0 && x + size.X > finalSize.X)
            {
                y += rowHeight + VSeparation;
                x = 0;
                rowHeight = 0;
            }

            child.Arrange(UIBox2.FromDimensions(x, y, size.X, size.Y));
            x += size.X + HSeparation;
            rowHeight = Math.Max(rowHeight, size.Y);
        }

        return finalSize;
    }
}
