using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using System.Numerics;

namespace Content.Client.ADT.Research.UI;

public sealed partial class DraggablePanel : LayoutContainer
{
    public DraggablePanel()
    {
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        foreach (var child in Children)
        {
            if (child is not ResearchConsoleItem item)
                continue;

            if (item.Prototype.RequiredTech.Count <= 0)
                continue;

            var list = Children.Where(x => x is ResearchConsoleItem second && item.Prototype.RequiredTech.Contains(second.Prototype.ID));

            foreach (var second in list)
            {
                var leftOffset = second.PixelPosition.Y;
                var rightOffset = item.PixelPosition.Y;

                var y1 = second.PixelPosition.Y + second.PixelHeight / 2 + leftOffset;
                var y2 = item.PixelPosition.Y + item.PixelHeight / 2 + rightOffset;

                if (second == item)
                {
                    handle.DrawLine(new Vector2(0, y1), new Vector2(PixelWidth, y2), Color.Cyan);
                    continue;
                }
                var startCoords = new Vector2(item.PixelPosition.X + 40, item.PixelPosition.Y + 40);
                var endCoords = new Vector2(second.PixelPosition.X + 40, second.PixelPosition.Y + 40);

                if (second.PixelPosition.Y != item.PixelPosition.Y)
                {

                    handle.DrawLine(startCoords, new(endCoords.X, startCoords.Y), Color.White);
                    handle.DrawLine(new(endCoords.X, startCoords.Y), endCoords, Color.White);
                }
                else
                {
                    handle.DrawLine(startCoords, endCoords, Color.White);
                }
            }
        }
    }
}
