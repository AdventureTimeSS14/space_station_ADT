using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTClampedContainer : Container
{
    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var width = 0f;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.X);
        }

        return new Vector2(width, 0);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        foreach (var child in Children)
        {
            child.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
        }

        return finalSize;
    }
}
