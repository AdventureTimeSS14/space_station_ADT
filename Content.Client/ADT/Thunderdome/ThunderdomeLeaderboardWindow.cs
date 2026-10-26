using System.Numerics;
using Content.Client.ADT.UserInterface.Controls;
using Content.Shared.ADT.Thunderdome;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Thunderdome;

public sealed partial class ThunderdomeLeaderboardWindow : ThunderdomeWindow
{
    private const int RankWidth = 34;
    private const int NumberWidth = 62;

    private readonly BoxContainer _contents;

    public ThunderdomeLeaderboardWindow()
    {
        WindowTitle = Loc.GetString("thunderdome-leaderboard-title");
        SetSize = new Vector2(520, 560);

        _contents = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
            Margin = new Thickness(10, 6),
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        scroll.AddChild(_contents);
        Contents.AddChild(scroll);

        var close = new ThunderdomeButton
        {
            Text = Loc.GetString("thunderdome-leaderboard-close"),
            Margin = new Thickness(8, 6),
        };
        close.OnPressed += Close;
        Contents.AddChild(close);
    }

    public void UpdateState(ThunderdomeLeaderboardEvent state)
    {
        _contents.RemoveAllChildren();

        if (state.Round != null)
            AddRoundSection(state.Round);

        AddHeader(Loc.GetString("thunderdome-leaderboard-top-header"));

        if (state.Top.Count == 0)
        {
            _contents.AddChild(new Label
            {
                Text = Loc.GetString("thunderdome-leaderboard-empty"),
                StyleClasses = { "LabelSubText" },
                Margin = new Thickness(4, 6),
            });
        }
        else
        {
            AddTableHeader();

            foreach (var entry in state.Top)
            {
                AddTableRow(entry);
            }
        }

        if (state.Personal != null)
            AddPersonalSection(state.Personal);
    }

    private void AddRoundSection(ThunderdomeRoundStats round)
    {
        AddHeader(Loc.GetString("thunderdome-leaderboard-round-header"));

        var panel = MakeCard();
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10, 6),
            SeparationOverride = 1,
        };
        panel.AddChild(box);

        if (round.Rank > 0)
        {
            box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-round-rank",
                ("rank", round.Rank),
                ("total", round.Participants))));
        }

        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-kills", ("kills", round.Kills))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-deaths", ("deaths", round.Deaths))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-kd", ("kd", FormatRatio(round.Kills, round.Deaths)))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-streak", ("streak", round.BestStreak))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-score", ("score", FormatScore(round.Score)))));

        if (round.DiscardedKills > 0)
        {
            box.AddChild(MakeValueLabel(
                Loc.GetString("thunderdome-leaderboard-discarded", ("count", round.DiscardedKills)),
                ThunderdomeTheme.AccentHover));

            box.AddChild(new Label
            {
                Text = Loc.GetString("thunderdome-leaderboard-discarded-hint"),
                StyleClasses = { "LabelSubText" },
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        _contents.AddChild(panel);
    }

    private void AddPersonalSection(ThunderdomePersonalStats personal)
    {
        AddHeader(Loc.GetString("thunderdome-leaderboard-personal-header"));

        var panel = MakeCard();
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10, 6),
            SeparationOverride = 1,
        };
        panel.AddChild(box);

        var rankText = personal.Rank > 0
            ? Loc.GetString("thunderdome-leaderboard-rank", ("rank", personal.Rank), ("total", personal.TotalRanked))
            : Loc.GetString("thunderdome-leaderboard-unranked");

        box.AddChild(MakeValueLabel(rankText, ThunderdomeTheme.AccentHover));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-score", ("score", FormatScore(personal.Score)))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-kills", ("kills", personal.Kills))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-deaths", ("deaths", personal.Deaths))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-kd", ("kd", FormatRatio(personal.Kills, personal.Deaths)))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-streak", ("streak", personal.BestStreak))));
        box.AddChild(MakeValueLabel(Loc.GetString("thunderdome-leaderboard-rounds", ("rounds", personal.RoundsPlayed))));

        _contents.AddChild(panel);
    }

    private void AddHeader(string text)
    {
        _contents.AddChild(new Label
        {
            Text = text,
            StyleClasses = { "LabelHeading" },
            FontColorOverride = ThunderdomeTheme.AccentHover,
            Margin = new Thickness(2, 10, 0, 4),
        });
    }

    private void AddTableHeader()
    {
        var row = MakeRowBox();

        row.AddChild(MakeCell(Loc.GetString("thunderdome-leaderboard-col-rank"), RankWidth, subdued: true));
        row.AddChild(MakeCell(Loc.GetString("thunderdome-leaderboard-col-name"), 0, subdued: true));
        row.AddChild(MakeCell(Loc.GetString("thunderdome-leaderboard-col-score"), NumberWidth, subdued: true));
        row.AddChild(MakeCell(Loc.GetString("thunderdome-leaderboard-col-kills"), NumberWidth, subdued: true));
        row.AddChild(MakeCell(Loc.GetString("thunderdome-leaderboard-col-deaths"), NumberWidth, subdued: true));
        row.AddChild(MakeCell(Loc.GetString("thunderdome-leaderboard-col-kd"), NumberWidth, subdued: true));

        _contents.AddChild(row);
    }

    private void AddTableRow(ThunderdomeLeaderboardEntry entry)
    {
        var row = MakeRowBox();

        row.AddChild(MakeCell($"{entry.Rank}", RankWidth, highlight: entry.IsSelf));
        row.AddChild(MakeCell(entry.Name, 0, highlight: entry.IsSelf));
        row.AddChild(MakeCell(FormatScore(entry.Score), NumberWidth, highlight: entry.IsSelf));
        row.AddChild(MakeCell($"{entry.Kills}", NumberWidth, highlight: entry.IsSelf));
        row.AddChild(MakeCell($"{entry.Deaths}", NumberWidth, highlight: entry.IsSelf));
        row.AddChild(MakeCell(FormatRatio(entry.Kills, entry.Deaths), NumberWidth, highlight: entry.IsSelf));

        if (!entry.IsSelf)
        {
            _contents.AddChild(row);
            return;
        }

        var panel = MakeCard();
        panel.AddChild(row);
        _contents.AddChild(panel);
    }

    private static BoxContainer MakeRowBox()
    {
        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(6, 2),
        };
    }

    private static PanelContainer MakeCard()
    {
        return new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = ThunderdomeTheme.CardBg,
                BorderColor = ThunderdomeTheme.AccentDim,
                BorderThickness = new Thickness(1),
            },
        };
    }

    private static Label MakeCell(string text, int width, bool subdued = false, bool highlight = false)
    {
        var label = new Label
        {
            Text = text,
            ClipText = true,
        };

        if (width > 0)
        {
            label.MinWidth = width;
        }
        else
        {
            label.HorizontalExpand = true;
        }

        if (subdued)
            label.StyleClasses.Add("LabelSubText");
        else if (highlight)
            label.FontColorOverride = ThunderdomeTheme.AccentHover;

        return label;
    }

    private static Label MakeValueLabel(string text, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
        };

        if (color != null)
            label.FontColorOverride = color.Value;

        return label;
    }

    private static string FormatScore(float score)
    {
        return score.ToString("0.#");
    }

    private static string FormatRatio(int kills, int deaths)
    {
        var ratio = deaths > 0 ? (float)kills / deaths : kills;
        return ratio.ToString("0.##");
    }
}
