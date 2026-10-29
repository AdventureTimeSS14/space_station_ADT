using System.Numerics;
using Content.Shared.ADT.Polls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollInviteWindow : DefaultWindow
{
    private readonly BoxContainer _list;

    public event Action<int>? OpenRequested;

    public PollInviteWindow()
    {
        Title = Loc.GetString("adt-poll-invite-title");
        MinSize = new Vector2(380, 140);
        Resizable = false;

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(6),
        };

        root.AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-invite-text"),
        });

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        root.AddChild(_list);

        var later = new Button
        {
            Text = Loc.GetString("adt-poll-invite-later"),
            HorizontalAlignment = HAlignment.Right,
        };

        later.OnPressed += _ => Close();
        root.AddChild(later);

        Contents.AddChild(root);
    }

    public void SetPolls(List<PollSummary> polls)
    {
        _list.RemoveAllChildren();

        foreach (var poll in polls)
        {
            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 6,
            };

            row.AddChild(new Label
            {
                Text = poll.Title,
                HorizontalExpand = true,
                ClipText = true,
                StyleClasses = { "LabelHeading" },
            });

            var open = new Button
            {
                Text = Loc.GetString("adt-poll-invite-open"),
                StyleClasses = { "Caution" },
            };

            var id = poll.Id;
            open.OnPressed += _ => OpenRequested?.Invoke(id);
            row.AddChild(open);

            _list.AddChild(row);
        }
    }
}
