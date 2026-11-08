using Content.Client.Message;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Guidebook.Ashwalkers;

public static class AshwalkerGuideWidgets
{
    public static Control BuildBarRow(string label, float percent, Color accent)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2),
        };

        row.AddChild(new Label
        {
            Text = label,
            MinWidth = 160,
            VerticalAlignment = Control.VAlignment.Center,
        });

        row.AddChild(new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = System.Math.Clamp(percent, 0f, 100f),
            MinHeight = 12,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center,
            BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = AshwalkerGuideColors.BarBackground },
            ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = accent },
        });

        var percentLabel = new RichTextLabel
        {
            MinWidth = 48,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = Control.VAlignment.Center,
        };
        percentLabel.SetMarkupPermissive($"[color={AshwalkerGuideColors.ToMarkup(accent)}]{percent:0.#}%[/color]");
        row.AddChild(percentLabel);

        return row;
    }

    public static Control BuildTextRow(string label, string value)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(0, 2, 0, 2),
        };

        row.AddChild(new Label
        {
            Text = label,
            MinWidth = 160,
            VerticalAlignment = Control.VAlignment.Center,
        });

        var valueLabel = new RichTextLabel
        {
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center,
        };
        valueLabel.SetMarkupPermissive(value);
        row.AddChild(valueLabel);

        return row;
    }
}
