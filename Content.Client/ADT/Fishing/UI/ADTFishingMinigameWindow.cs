using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Fishing.UI;

public sealed class ADTFishingMinigameWindow : DefaultWindow
{
    private readonly ADTFishingTrack _track;

    public event Action<bool>? OnHold;

    public ADTFishingMinigameWindow()
    {
        Title = Loc.GetString("adt-fishing-minigame-title");
        MinSize = new Vector2(260, 340);
        SetSize = new Vector2(260, 340);

        _track = new ADTFishingTrack
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _track.OnHold += holding => OnHold?.Invoke(holding);

        var hint = new Label
        {
            Text = Loc.GetString("adt-fishing-minigame-hint"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
        };

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        box.AddChild(hint);
        box.AddChild(_track);

        Contents.AddChild(box);
    }

    public void SetRod(EntityUid rod)
    {
        _track.Rod = rod;
    }
}
