using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Polls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollListWindow : DefaultWindow
{
    private readonly BoxContainer _list;

    public event Action<int>? OpenRequested;

    public PollListWindow()
    {
        Title = Loc.GetString("adt-poll-list-title");
        MinSize = new Vector2(520, 320);
        SetSize = new Vector2(560, 420);

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(6),
        };

        Contents.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _list },
        });
    }

    public void SetPolls(IReadOnlyList<PollSummary> polls)
    {
        _list.RemoveAllChildren();

        if (polls.Count == 0)
        {
            _list.AddChild(new Label
            {
                Text = Loc.GetString("adt-poll-list-empty"),
                FontColorOverride = Color.Gray,
            });

            return;
        }

        var ordered = polls
            .OrderBy(p => p.Answered)
            .ThenByDescending(p => p.Targeted)
            .ThenByDescending(p => p.Id);

        foreach (var poll in ordered)
        {
            _list.AddChild(BuildRow(poll));
        }
    }

    private Control BuildRow(PollSummary poll)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        var info = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        info.AddChild(new Label
        {
            Text = poll.Title,
            ClipText = true,
            StyleClasses = { "LabelHeading" },
        });

        var details = new List<string>();

        if (poll.Targeted)
            details.Add(Loc.GetString("adt-poll-list-targeted"));

        if (poll.EndsAt is { } endsAt)
            details.Add(Loc.GetString("adt-poll-list-ends", ("date", endsAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"))));

        if (details.Count > 0)
        {
            info.AddChild(new Label
            {
                Text = string.Join(" | ", details),
                FontColorOverride = poll.Targeted ? Color.Gold : Color.Gray,
            });
        }

        row.AddChild(info);

        var open = new Button
        {
            Text = poll.Answered
                ? Loc.GetString("adt-poll-list-answered")
                : Loc.GetString("adt-poll-list-open"),
            Disabled = poll.Answered,
            VerticalAlignment = VAlignment.Center,
        };

        var id = poll.Id;
        open.OnPressed += _ => OpenRequested?.Invoke(id);
        row.AddChild(open);

        var panel = new PanelContainer
        {
            StyleClasses = { "BackgroundDark" },
        };

        row.Margin = new Thickness(4);
        panel.AddChild(row);
        return panel;
    }
}
