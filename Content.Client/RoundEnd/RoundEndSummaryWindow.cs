using System.Linq;
using System.Numerics;
using Content.Client.Message;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
// Goob Station - End of Round Screen
using Content.Client.Stylesheets;
using Content.Shared.ADT.RoundEnd; // ADT-Tweak
using Content.Shared.Mobs;

namespace Content.Client.RoundEnd
{
    public sealed class RoundEndSummaryWindow : DefaultWindow
    {
    private readonly IEntityManager _entityManager;
    private readonly List<RoundEndStatEntry> _roundReport;
    private readonly Dictionary<string, int> _speciesCensus;
    public int RoundId;

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, int roundId,
            RoundEndMessageEvent.RoundEndPlayerInfo[] info, IEntityManager entityManager,
            List<RoundEndStatEntry>? roundReport = null,
            Dictionary<string, int>? speciesCensus = null)
        {
            _entityManager = entityManager;
            _roundReport = roundReport ?? new List<RoundEndStatEntry>();
            _speciesCensus = speciesCensus ?? new Dictionary<string, int>();

            MinSize = SetSize = new Vector2(560, 620);

            Title = Loc.GetString("round-end-summary-window-title");

            // The round end window is split into two tabs, one about the round stats
            // and the other is a list of RoundEndPlayerInfo for each player.
            // This tab would be a good place for things like: "x many people died.",
            // "clown slipped the crew x times.", "x shots were fired this round.", etc.
            // Also good for serious info.

            RoundId = roundId;
            var roundEndTabs = new TabContainer();
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId));
            roundEndTabs.AddChild(MakePlayerManifestTab(info));
            roundEndTabs.AddChild(MakeCrewTableTab(info)); // ADT-Tweak
            roundEndTabs.AddChild(MakeStatsTab(gm, roundTimeSpan, roundId)); // ADT-Tweak

            ContentsContainer.AddChild(roundEndTabs);

            OpenCenteredRight();
            MoveToFront();
        }

        private BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration, int roundId)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            var gamemodeMessage = new FormattedMessage();
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-round-id-label", ("roundId", roundId)));
            gamemodeMessage.AddText(" ");
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
            gamemodeLabel.SetMessage(gamemodeMessage);
            roundEndSummaryContainer.AddChild(gamemodeLabel);

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours", roundDuration.Hours),
                                                   ("minutes", roundDuration.Minutes),
                                                   ("seconds", roundDuration.Seconds)));
            roundEndSummaryContainer.AddChild(roundTimeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                roundEndLabel.SetMessage(FormattedMessage.FromMarkupPermissive(roundEnd), tagsAllowed: null);
                roundEndSummaryContainer.AddChild(roundEndLabel);
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }

        // ADT-Tweak-start
        //всё в этом регионе сильно модифицировано
        [Obsolete("This is only used for the end of round summary, and is not intended to be used for anything else. It will be removed once we have a better way to track this information.")]
        private BoxContainer MakePlayerManifestTab(RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            var playerManifestTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-player-manifest-tab-title")
            };

            var playerInfoContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var playerInfoContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Put observers at the bottom of the list. Put antags on top.
            var sortedPlayersInfo = playersInfo.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);

            //Create labels for each player info.
            foreach (var playerInfo in sortedPlayersInfo)
            {
                var panel = new PanelContainer
                {
                    StyleClasses = { StyleNano.StyleClassBackgroundBaseDark },
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    VerticalExpand = true
                };

                if (playerInfo.PlayerNetEntity != null)
                {
                    hBox.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
                    {
                        OverrideDirection = Direction.South,
                        VerticalAlignment = VAlignment.Center,
                        SetSize = new Vector2(64, 64),
                        VerticalExpand = true,
                        Stretch = SpriteView.StretchMode.Fill,
                        Margin = new Thickness(3, 0, 3, 0)
                    });
                }

                var textVBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    VerticalExpand = true,
                    SeparationOverride = 2,
                };

                var playerTitleBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var playerInfoText = new RichTextLabel
                {
                    VerticalAlignment = VAlignment.Center,
                    VerticalExpand = true,
                };

                // if (playerInfo.PlayerNetEntity != null)
                // {
                //     hBox.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
                //         {
                //             OverrideDirection = Direction.South,
                //             VerticalAlignment = VAlignment.Center,
                //             SetSize = new Vector2(32, 32),
                //             VerticalExpand = true,
                //         });
                // }

                if (playerInfo.PlayerICName != null)
                {
                    var playerNameText = new Label
                    {
                        VerticalAlignment = VAlignment.Bottom,
                        StyleClasses = { StyleNano.StyleClassLabelHeading },
                        Margin = new Thickness(0, 0, 6, 0),
                        Text = playerInfo.PlayerICName
                    };
                    playerTitleBox.AddChild(playerNameText);

                    var role = Loc.GetString(playerInfo.Role);
                    var playerRoleText = new Label
                    {
                        VerticalAlignment = VAlignment.Bottom,
                        StyleClasses = { StyleNano.StyleClassLabelSubText },
                        Text = Loc.GetString("round-end-summary-window-player-name",
                            ("player", playerInfo.PlayerOOCName))
                    };

                    if (role != "Unknown")
                        playerRoleText.Text = Loc.GetString("round-end-summary-window-player-name-role",
                                ("role", role),
                                ("player", playerInfo.PlayerOOCName));

                    playerTitleBox.AddChild(playerRoleText);
                }

                textVBox.AddChild(playerTitleBox);

                if (!string.IsNullOrWhiteSpace(playerInfo.LastWords))
                {
                    var playerLastWordsText = new RichTextLabel
                    {
                        VerticalAlignment = VAlignment.Center,
                        VerticalExpand = true,
                    };

                    playerLastWordsText.SetMarkup(Loc.GetString("round-end-summary-window-last-words",
                        ("lastWords", playerInfo.LastWords)));

                    textVBox.AddChild(playerLastWordsText);
                }

                var hDeathBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var deathLabel = new RichTextLabel
                {
                    VerticalAlignment = VAlignment.Center,
                    VerticalExpand = true,
                };

                textVBox.AddChild(deathLabel);

                if (playerInfo.EntMobState == MobState.Dead
                    && playerInfo.DamagePerGroup.Values.Any(v => v > 0))
                {
                    var totalDamage = playerInfo.DamagePerGroup.Values.Sum(static v => (decimal)v);
                    var severityAdj = totalDamage switch
                    {
                        >= 1000 => Loc.GetString("brutal-damage-death-round-end"),
                        >= 750 => Loc.GetString("painful-damage-death-round-end"),
                        >= 500 => Loc.GetString("agony-damage-death-round-end"),
                        >= 300 => Loc.GetString("breakable-damage-death-round-end"),
                        >= 200 => Loc.GetString("hurt-damage-death-round-end"),
                        _ => Loc.GetString("unknown-damage-death-round-end")
                    };

                    var highestDamage = playerInfo.DamagePerGroup
                        .OrderByDescending(kvp => kvp.Value)
                        .First();

                    var typeAdj = highestDamage.Key.Id switch
                    {
                        "Burn" => Loc.GetString("burn-death-round-end"),
                        "Brute" => Loc.GetString("brute-death-round-end"),
                        "Toxin" => Loc.GetString("toxin-death-round-end"),
                        "Airloss" => Loc.GetString("airloss-death-round-end"),
                        "Genetic" => Loc.GetString("genetic-death-round-end"),
                        "Metaphysical" => Loc.GetString("metaphysical-death-round-end"),
                        "Electronic" => Loc.GetString("electronic-death-round-end"),
                        _ => Loc.GetString("mysterious-death-round-end"),
                    };

                    deathLabel.SetMarkup(
                        Loc.GetString("round-end-summary-window-death",
                            ("severity", severityAdj),
                            ("type", typeAdj)));

                    var damageTable = new GridContainer
                    {
                        Columns = playerInfo.DamagePerGroup.Count,
                    };

                    foreach (var damage in playerInfo.DamagePerGroup)
                    {
                        if (damage.Value <= 0)
                            continue;

                        var color = damage.Key.Id switch
                        {
                            "Burn" => Color.Orange,
                            "Brute" => Color.Red,
                            "Toxin" => Color.Green,
                            "Airloss" => Color.Blue,
                            "Genetic" => Color.Cyan,
                            "Metaphysical" => Color.Purple,
                            "Electronic" => Color.DarkOrange,
                            _ => Color.White,
                        };
                        var damagePanel = new PanelContainer
                        {
                            StyleClasses = { StyleNano.StyleClassBackgroundBaseLight },
                            Margin = new Thickness(2, 2, 2, 2)
                        };
                        var damageBox = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Vertical,
                            Margin = new Thickness(1)
                        };
                        var valueLabel = new Label
                        {
                            Text = Math.Round((float)damage.Value).ToString(),
                            FontColorOverride = color,
                            HorizontalAlignment = HAlignment.Center,
                            VerticalAlignment = VAlignment.Center,
                        };
                        var headerLabel = new Label
                        {
                            Text = damage.Key,
                            FontColorOverride = Color.Gray,
                            HorizontalAlignment = HAlignment.Center,
                            VerticalAlignment = VAlignment.Center,
                        };
                        damagePanel.AddChild(damageBox);
                        damageBox.AddChild(valueLabel);
                        damageBox.AddChild(headerLabel);
                        damageTable.AddChild(damagePanel);
                    }

                    textVBox.AddChild(damageTable);
                }
                else if (playerInfo.EntMobState == MobState.Invalid)
                {
                    deathLabel.SetMarkup(Loc.GetString("round-end-summary-window-death-unknown"));
                }

                hBox.AddChild(textVBox);
                panel.AddChild(hBox);
                playerInfoContainer.AddChild(panel);
            }

        playerInfoContainerScrollbox.AddChild(playerInfoContainer);
        playerManifestTab.AddChild(playerInfoContainerScrollbox);

        return playerManifestTab;
    }

    // ADT-Tweak-start
    private BoxContainer MakeStatsTab(string gamemode, TimeSpan roundDuration, int roundId)
    {
        var statsTab = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Name = Loc.GetString("round-end-report-tab-title")
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            Margin = new Thickness(10)
        };

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2
        };

        AddReportLine(container, Loc.GetString("round-end-report-round-id", ("roundId", roundId)));
        AddReportLine(container, Loc.GetString("round-end-report-gamemode", ("gamemode", gamemode)));
        AddReportLine(container, Loc.GetString("round-end-report-duration",
            ("hours", roundDuration.Hours),
            ("minutes", roundDuration.Minutes),
            ("seconds", roundDuration.Seconds)));

        AddReportCategory(container, RoundEndStatCategory.Summary, "round-end-report-category-summary");
        AddReportCategory(container, RoundEndStatCategory.FirstDeath, "round-end-report-category-first-death");
        AddReportCategory(container, RoundEndStatCategory.Economy, "round-end-report-category-economy");
        AddReportCategory(container, RoundEndStatCategory.Misc, "round-end-report-category-misc");
        AddSpeciesCensus(container);

        scroll.AddChild(container);
        statsTab.AddChild(scroll);

        return statsTab;
    }

    private void AddReportCategory(BoxContainer container, RoundEndStatCategory category, string headerLocId)
    {
        var entries = _roundReport
            .Where(e => e.Category == category)
            .OrderBy(e => e.Order)
            .ToArray();

        if (entries.Length == 0)
            return;

        AddCategoryHeader(container, headerLocId);

        foreach (var entry in entries)
        {
            AddReportLine(container, FormatReportEntry(entry), 8);
        }
    }

    private void AddReportLine(BoxContainer container, string markup, int indent = 0)
    {
        var label = new RichTextLabel { Margin = new Thickness(indent, 0, 0, 0) };
        label.SetMarkup(markup);
        container.AddChild(label);
    }

    private void AddCategoryHeader(BoxContainer container, string headerLocId)
    {
        var label = new Label
        {
            Text = Loc.GetString(headerLocId),
            StyleClasses = { StyleNano.StyleClassLabelHeading },
            Margin = new Thickness(0, 8, 0, 2)
        };
        container.AddChild(label);
    }

    /// <summary>
    ///     Resolves a report line, translating any locale-id arguments client-side.
    /// </summary>
    private static string FormatReportEntry(RoundEndStatEntry entry)
    {
        var args = new List<(string, object)>();

        foreach (var (key, value) in entry.Args)
            args.Add((key, value));

        foreach (var (key, locId) in entry.LocArgs)
            args.Add((key, Loc.GetString(locId)));

        return Loc.GetString(entry.LocId, args.ToArray());
    }

    private void AddSpeciesCensus(BoxContainer container)
    {
        if (_speciesCensus.Count == 0)
            return;

        AddCategoryHeader(container, "round-end-report-category-census");

        AddReportLine(container, Loc.GetString("round-end-report-species-header",
            ("count", _speciesCensus.Count)), 8);

        foreach (var (species, count) in _speciesCensus.OrderByDescending(p => p.Value))
        {
            var name = Loc.TryGetString($"species-name-{species.ToLowerInvariant()}", out var localized)
                ? localized
                : species;

            AddReportLine(container, Loc.GetString("round-end-report-species-line",
                ("species", name), ("count", count)), 16);
        }
    }
    // ADT-Tweak-end

    // ADT-Tweak-start
    private BoxContainer MakeCrewTableTab(RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
    {
        var crewTab = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Name = Loc.GetString("round-end-summary-window-crew-tab-title")
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            Margin = new Thickness(10)
        };

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical
        };

        var crew = playersInfo.Where(p => !p.Observer).ToArray();
        var observers = playersInfo.Where(p => p.Observer).ToArray();

        var alive = crew.Count(p => p.EntMobState != MobState.Dead && p.EntMobState != MobState.Invalid);
        var dead = crew.Count(p => p.EntMobState == MobState.Dead);
        var escaped = crew.Count(p => p.Escaped && p.EntMobState != MobState.Dead);

        var summaryLabel = new RichTextLabel { Margin = new Thickness(0, 0, 0, 8) };
        summaryLabel.SetMarkup(Loc.GetString("round-end-summary-window-crew-summary",
            ("alive", alive), ("dead", dead), ("escaped", escaped), ("total", crew.Length)));
        container.AddChild(summaryLabel);

        var grid = new GridContainer
        {
            Columns = 3,
            HorizontalExpand = true
        };

        void AddHeader(string text)
        {
            var label = new Label
            {
                Text = text,
                StyleClasses = { StyleNano.StyleClassLabelHeading },
                Margin = new Thickness(4, 2)
            };
            grid.AddChild(label);
        }

        AddHeader(Loc.GetString("round-end-summary-window-crew-name-header"));
        AddHeader(Loc.GetString("round-end-summary-window-crew-role-header"));
        AddHeader(Loc.GetString("round-end-summary-window-crew-status-header"));

        var sorted = crew
            .OrderBy(p => p.EntMobState == MobState.Dead)
            .ThenBy(p => p.PlayerICName ?? p.PlayerOOCName)
            .Concat(observers.OrderBy(p => p.PlayerICName ?? p.PlayerOOCName));

        foreach (var player in sorted)
        {
            var name = player.PlayerICName ?? player.PlayerOOCName;

            var nameLabel = new Label
            {
                Text = player.Antag ? $"{name} [?]" : name,
                FontColorOverride = player.Antag ? Color.Red : (player.Observer ? Color.Gray : Color.White),
                Margin = new Thickness(4, 1)
            };
            grid.AddChild(nameLabel);

            var roleLabel = new Label
            {
                Text = Loc.GetString(player.Role),
                FontColorOverride = player.Observer ? Color.Gray : Color.LightGray,
                Margin = new Thickness(4, 1)
            };
            grid.AddChild(roleLabel);

            string status;
            Color statusColor;
            if (player.Observer)
            {
                status = Loc.GetString("round-end-summary-window-crew-status-observer");
                statusColor = Color.Gray;
            }
            else if (player.EntMobState == MobState.Dead)
            {
                status = Loc.GetString("round-end-summary-window-crew-status-dead");
                statusColor = Color.Red;
            }
            else if (player.EntMobState == MobState.Invalid)
            {
                status = Loc.GetString("round-end-summary-window-crew-status-nobody");
                statusColor = Color.Gray;
            }
            else if (player.Escaped)
            {
                status = Loc.GetString("round-end-summary-window-crew-status-escaped");
                statusColor = Color.Green;
            }
            else
            {
                status = Loc.GetString("round-end-summary-window-crew-status-alive");
                statusColor = Color.Yellow;
            }

            var statusLabel = new Label
            {
                Text = status,
                FontColorOverride = statusColor,
                Margin = new Thickness(4, 1)
            };
            grid.AddChild(statusLabel);
        }

        container.AddChild(grid);
        scroll.AddChild(container);
        crewTab.AddChild(scroll);

        return crewTab;
    }
    // ADT-Tweak-end
    }
    // ADT-Tweak-end
}
