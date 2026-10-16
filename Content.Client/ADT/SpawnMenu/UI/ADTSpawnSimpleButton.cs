using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTSpawnSimpleButton : Control, IADTSpawnItem
{
    public int Index { get; }
    public Button ActualButton { get; }
    public Control Preview { get; }

    public ADTSpawnSimpleButton(int index, bool grid, Control preview, string text, string tooltip, Control? extra = null)
    {
        Index = index;
        Preview = preview;

        ActualButton = new Button
        {
            ToggleMode = true,
            ToolTip = tooltip,
        };
        AddChild(ActualButton);

        preview.SetSize = grid ? new Vector2(48, 48) : new Vector2(32, 32);
        preview.HorizontalAlignment = HAlignment.Center;
        preview.VerticalAlignment = VAlignment.Center;
        preview.MouseFilter = MouseFilterMode.Ignore;

        var label = new Label
        {
            Text = text,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
        };

        if (grid)
        {
            label.Align = Label.AlignMode.Center;
            label.StyleClasses.Add("LabelSubText");

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Margin = new Thickness(2),
                Children =
                {
                    preview,
                    label,
                },
            });
            return;
        }

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Children =
            {
                preview,
                label,
            },
        };

        if (extra != null)
            row.AddChild(extra);

        AddChild(row);
    }
}
