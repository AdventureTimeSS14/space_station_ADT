using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTEntitySpawnButton : Control, IADTSpawnItem
{
    public int Index { get; }
    public ADTSpawnListItem Item { get; }
    public Button ActualButton { get; }

    public event Action<string, string>? OnTagPressed;

    public ADTEntitySpawnButton(ADTSpawnListItem item, int index, bool grid, string tooltip)
    {
        Item = item;
        Index = index;

        var prototype = item.Representative.Prototype;
        var name = string.IsNullOrEmpty(prototype.Name) ? prototype.ID : prototype.Name;

        ActualButton = new Button
        {
            ToggleMode = true,
            ToolTip = tooltip,
        };
        AddChild(ActualButton);

        var preview = new ADTSpawnPreviewView
        {
            SetSize = grid ? new Vector2(48, 48) : new Vector2(32, 32),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Stretch = SpriteView.StretchMode.Fill,
        };
        preview.SetPrototype(prototype.ID);

        var label = new Label
        {
            Text = name,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
        };

        if (grid)
        {
            label.Align = Label.AlignMode.Center;
            label.StyleClasses.Add("LabelSubText");

            if (item.Variants.Count > 1)
                label.Text = Loc.GetString("adt-spawn-menu-variants-short", ("name", name), ("count", item.Variants.Count));

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

        var chips = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 3,
            Margin = new Thickness(0, 0, 14, 0),
        };

        if (item.Variants.Count > 1)
        {
            var variants = new ADTSpawnTagChip(string.Empty,
                Loc.GetString("adt-spawn-menu-variants-chip", ("count", item.Variants.Count)));
            variants.MouseFilter = MouseFilterMode.Ignore;
            chips.AddChild(variants);
        }
        else
        {
            foreach (var (raw, normalized) in item.Representative.SuffixTags)
            {
                var chip = new ADTSpawnTagChip(normalized, raw);
                chip.OnPressed += _ => OnTagPressed?.Invoke(raw, normalized);
                chips.AddChild(chip);
            }
        }

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Children =
            {
                preview,
                label,
                chips,
            },
        });
    }
}
