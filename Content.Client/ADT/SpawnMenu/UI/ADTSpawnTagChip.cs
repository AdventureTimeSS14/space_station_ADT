using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTSpawnTagChip : ContainerButton
{
    private static readonly Color[] Palette =
    {
        Color.FromHex("#3B5A7A"),
        Color.FromHex("#5A3B7A"),
        Color.FromHex("#3B7A5A"),
        Color.FromHex("#7A5A3B"),
        Color.FromHex("#7A3B4F"),
        Color.FromHex("#3B6F7A"),
        Color.FromHex("#6A7A3B"),
        Color.FromHex("#4F4F6A"),
    };

    public readonly string Tag;

    public ADTSpawnTagChip(string tag, string text, bool active = false)
    {
        Tag = tag;
        VerticalAlignment = VAlignment.Center;

        var color = Palette[GetStableHash(tag) % Palette.Length];
        if (active)
            color = Color.FromHex("#9C7A2E");

        AddChild(new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color,
                ContentMarginLeftOverride = 4,
                ContentMarginRightOverride = 4,
                ContentMarginTopOverride = 0,
                ContentMarginBottomOverride = 0,
            },
            Children =
            {
                new Label
                {
                    Text = text,
                    StyleClasses = { "LabelSubText" },
                    FontColorOverride = Color.White,
                },
            },
        });
    }

    private static int GetStableHash(string text)
    {
        var hash = 17;
        foreach (var ch in text)
        {
            hash = unchecked(hash * 31 + ch);
        }

        return Math.Abs(hash % 1000);
    }
}
