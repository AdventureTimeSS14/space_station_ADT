using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Polls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollStatsView : BoxContainer
{
    private readonly IPrototypeManager _proto;

    private readonly Label _summary;
    private readonly OptionButton _groupBy;
    private readonly BoxContainer _questions;
    private readonly BoxContainer _responses;
    private readonly Button _prev;
    private readonly Button _next;
    private readonly Label _page;

    private PollStats? _stats;

    public int? PollId => _stats?.PollId;

    public event Action<PollGroupBy, int>? StatsRequested;

    public PollStatsView(IPrototypeManager proto)
    {
        _proto = proto;

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
        Margin = new Thickness(6);

        var top = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        _summary = new Label
        {
            HorizontalExpand = true,
            StyleClasses = { "LabelHeading" },
        };

        _groupBy = new OptionButton
        {
            MinWidth = 180,
        };

        _groupBy.AddItem(Loc.GetString("adt-poll-stats-group-by-none"), (int) PollGroupBy.None);
        _groupBy.AddItem(Loc.GetString("adt-poll-stats-group-by-department"), (int) PollGroupBy.Department);
        _groupBy.AddItem(Loc.GetString("adt-poll-stats-group-by-job"), (int) PollGroupBy.Job);
        _groupBy.AddItem(Loc.GetString("adt-poll-stats-group-by-species"), (int) PollGroupBy.Species);

        _groupBy.OnItemSelected += args =>
        {
            _groupBy.SelectId(args.Id);
            StatsRequested?.Invoke((PollGroupBy) args.Id, _stats?.PageIndex ?? 0);
        };

        top.AddChild(_summary);
        top.AddChild(_groupBy);
        AddChild(top);

        _questions = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10,
        };

        AddChild(_questions);

        AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-stats-responses"),
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(0, 10, 0, 2),
        });

        var pager = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        _prev = new Button
        {
            Text = "<",
            StyleClasses = { "OpenRight" },
        };

        _page = new Label
        {
            MinWidth = 80,
            Align = Label.AlignMode.Center,
        };

        _next = new Button
        {
            Text = ">",
            StyleClasses = { "OpenLeft" },
        };

        _prev.OnPressed += _ => RequestPage(-1);
        _next.OnPressed += _ => RequestPage(1);

        pager.AddChild(_prev);
        pager.AddChild(_page);
        pager.AddChild(_next);
        AddChild(pager);

        _responses = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };

        AddChild(_responses);
    }

    private void RequestPage(int delta)
    {
        if (_stats == null)
            return;

        StatsRequested?.Invoke(_stats.GroupBy, _stats.PageIndex + delta);
    }

    public void SetStats(PollData poll, PollStats stats)
    {
        _stats = stats;

        _summary.Text = Loc.GetString("adt-poll-stats-summary",
            ("title", poll.Title),
            ("total", stats.Total),
            ("targeted", stats.Targeted),
            ("voluntary", stats.Total - stats.Targeted));

        _groupBy.SelectId((int) stats.GroupBy);

        _questions.RemoveAllChildren();

        for (var i = 0; i < poll.Questions.Count && i < stats.Questions.Count; i++)
        {
            _questions.AddChild(BuildQuestion(i, poll.Questions[i], stats.Questions[i], stats.GroupBy));
        }

        _page.Text = $"{stats.PageIndex + 1} / {stats.PageCount}";
        _prev.Disabled = stats.PageIndex <= 0;
        _next.Disabled = stats.PageIndex >= stats.PageCount - 1;

        _responses.RemoveAllChildren();

        foreach (var response in stats.Page)
        {
            _responses.AddChild(BuildResponse(poll, response));
        }
    }

    private Control BuildQuestion(int index, PollQuestion question, PollQuestionStats stats, PollGroupBy groupBy)
    {
        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        box.AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-stats-question",
                ("index", index + 1),
                ("text", question.Text),
                ("answered", stats.Answered)),
            StyleClasses = { "LabelHeading" },
        });

        if (question.Type == PollQuestionType.Text)
        {
            box.AddChild(new Label
            {
                Text = Loc.GetString("adt-poll-stats-text-below"),
                FontColorOverride = Color.Gray,
            });

            return box;
        }

        var max = Math.Max(1, stats.Answered);

        for (var i = 0; i < stats.Counts.Length; i++)
        {
            box.AddChild(BuildBar(BucketName(question, i), stats.Counts[i], max));
        }

        if (question.Type == PollQuestionType.Rating)
        {
            box.AddChild(new Label
            {
                Text = Loc.GetString("adt-poll-stats-average", ("value", Average(question, stats.Counts).ToString("0.00"))),
            });
        }

        if (groupBy == PollGroupBy.None || stats.Groups.Count == 0)
            return box;

        foreach (var (group, counts) in stats.Groups.OrderByDescending(g => g.Value.Sum()))
        {
            box.AddChild(new Label
            {
                Text = $"{PollNames.Group(_proto, groupBy, group)}: {DescribeGroup(question, counts)}",
                FontColorOverride = Color.LightGray,
                Margin = new Thickness(12, 0, 0, 0),
            });
        }

        return box;
    }

    private static Control BuildBar(string name, int count, int max)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };

        row.AddChild(new Label
        {
            Text = name,
            MinWidth = 200,
            MaxWidth = 200,
            ClipText = true,
        });

        row.AddChild(new ProgressBar
        {
            MinValue = 0,
            MaxValue = max,
            Value = count,
            HorizontalExpand = true,
            MinSize = new Vector2(100, 16),
            VerticalAlignment = VAlignment.Center,
        });

        row.AddChild(new Label
        {
            Text = $"{count} ({count * 100 / max}%)",
            MinWidth = 80,
        });

        return row;
    }

    private static string BucketName(PollQuestion question, int bucket)
    {
        if (question.Type == PollQuestionType.Rating)
            return (question.RatingMin + bucket).ToString();

        return bucket < question.Options.Count ? question.Options[bucket] : bucket.ToString();
    }

    private static float Average(PollQuestion question, int[] counts)
    {
        var total = 0;
        var sum = 0;

        for (var i = 0; i < counts.Length; i++)
        {
            total += counts[i];
            sum += counts[i] * (question.RatingMin + i);
        }

        return total == 0 ? 0f : (float) sum / total;
    }

    private static string DescribeGroup(PollQuestion question, int[] counts)
    {
        if (question.Type == PollQuestionType.Rating)
        {
            return Loc.GetString("adt-poll-stats-group-rating",
                ("value", Average(question, counts).ToString("0.00")),
                ("count", counts.Sum()));
        }

        var parts = new List<string>();

        for (var i = 0; i < counts.Length; i++)
        {
            if (counts[i] > 0)
                parts.Add($"{BucketName(question, i)} - {counts[i]}");
        }

        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }

    private Control BuildResponse(PollData poll, PollResponseView response)
    {
        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Margin = new Thickness(6),
        };

        var round = response.RoundId?.ToString() ?? "-";
        var kind = response.Targeted
            ? Loc.GetString("adt-poll-stats-targeted")
            : Loc.GetString("adt-poll-stats-voluntary");

        box.AddChild(new Label
        {
            Text = Loc.GetString("adt-poll-stats-response-header",
                ("name", response.UserName),
                ("date", response.SubmittedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")),
                ("round", round),
                ("server", response.Server),
                ("kind", kind)),
            StyleClasses = { "LabelHeading" },
        });

        box.AddChild(WrappedLabel(DescribeSnapshot(response.Snapshot), Color.Gray));

        for (var i = 0; i < poll.Questions.Count; i++)
        {
            var answer = i < response.Answers.Count ? response.Answers[i] : new PollAnswer();

            box.AddChild(WrappedLabel($"{i + 1}. {DescribeAnswer(poll.Questions[i], answer)}", Color.White));
        }

        var panel = new PanelContainer
        {
            StyleClasses = { "BackgroundDark" },
        };

        panel.AddChild(box);
        return panel;
    }

    private static RichTextLabel WrappedLabel(string text, Color color)
    {
        var message = new FormattedMessage();
        message.PushColor(color);
        message.AddText(text);
        message.Pop();

        var label = new RichTextLabel();
        label.SetMessage(message);
        return label;
    }

    private string DescribeSnapshot(PollSnapshot snapshot)
    {
        var parts = new List<string>();

        if (snapshot.Jobs.Count > 0)
            parts.Add(string.Join(", ", snapshot.Jobs.Select(j => PollNames.Job(_proto, j))));

        if (snapshot.Departments.Count > 0)
            parts.Add(string.Join(", ", snapshot.Departments.Select(d => PollNames.Department(_proto, d))));

        if (snapshot.Species.Count > 0)
            parts.Add(string.Join(", ", snapshot.Species.Select(s => PollNames.Species(_proto, s))));

        if (snapshot.AntagRoles.Count > 0)
            parts.Add(string.Join(", ", snapshot.AntagRoles.Select(a => PollNames.Antag(_proto, a))));

        if (!snapshot.InRound)
            parts.Add(Loc.GetString("adt-poll-stats-not-in-round"));

        parts.Add(Loc.GetString("adt-poll-stats-playtime", ("hours", snapshot.PlaytimeMinutes / 60)));

        return string.Join(" | ", parts);
    }

    private static string DescribeAnswer(PollQuestion question, PollAnswer answer)
    {
        if (answer.IsEmpty())
            return Loc.GetString("adt-poll-stats-no-answer");

        switch (question.Type)
        {
            case PollQuestionType.Single:
            case PollQuestionType.Multiple:
                if (answer.Options == null)
                    return "-";

                return string.Join(", ", answer.Options.Select(o => BucketName(question, o)));

            case PollQuestionType.Rating:
                return answer.Rating?.ToString() ?? "-";

            default:
                return answer.Text ?? "-";
        }
    }
}
