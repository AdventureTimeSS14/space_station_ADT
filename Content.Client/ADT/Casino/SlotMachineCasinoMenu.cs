using System.Numerics;
using Robust.Shared.Graphics.RSI;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Casino;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Casino;

public sealed class SlotMachineCasinoMenu : FancyWindow
{
    private readonly SlotMachineCasinoBoundUserInterface _bui;
    private readonly IResourceCache _resCache;
    private readonly SlotMachineFonts _fonts;
    


    private readonly ReelsControl _reels = new();
    private readonly Label _jackpotAmount = new();
    private readonly Label _bonusBanner = new();
    private readonly Label _streakBanner = new();
    private readonly Label _nearMissBanner = new();
    private readonly Label _freeSpinsBanner = new();
    private readonly Label _hotBanner = new();
    private readonly Label _balanceLabel = new();
    private readonly Label _sessionLabel = new();
    private readonly Button _spinButton = new();
    private readonly Button _cashOutButton = new();
    private readonly Button _stopAutoButton = new();
    private readonly BoxContainer _autoRow = new() { Orientation = BoxContainer.LayoutOrientation.Horizontal };
    private readonly Label _statsPlays = new();
    private readonly Label _statsJackpots = new();
    private readonly Label _statsStreak = new();
    private readonly Label _paytableFive = new();
    private readonly Label _paytableFour = new();
    private readonly Button[] _betButtons = new Button[5];
    private readonly PanelContainer _gamblePanel = new() { Visible = false };
    private readonly Label _gambleTitle = new();
    private readonly Control _mainContent;
    private int _lastBet = -1;
    private readonly ResultOverlayControl _resultOverlay = new();
    private SlotMachineCasinoBoundUserInterfaceState? _state;
    private int _lastSpinCount = -1;
    private (string Text, Color Color, int Size)? _pendingResult;

    private readonly Texture? _sevenIcon;

    public SlotMachineCasinoMenu(SlotMachineCasinoBoundUserInterface bui)
    {
        _bui = bui;
        IoCManager.InjectDependencies(this);
        _resCache = IoCManager.Resolve<IResourceCache>();
        _fonts = new SlotMachineFonts(_resCache);

        Title = Loc.GetString("slotmachine-casino-title");
        SetSize = new Vector2(540, 680);

        foreach (var sym in SlotMachineCasinoComponent.SymbolIds)
        {
            if (GetRsiTexture(SymbolsRsiPath, sym) is { } tex)
                _reels.Symbols[sym] = tex;
        }

        _sevenIcon = GetRsiTexture(SymbolsRsiPath, "seven");


        _mainContent = BuildMainUi();
        ContentsContainer.AddChild(_mainContent);

        var intro = BuildIntro();
        _intro = intro;
        ContentsContainer.AddChild(_intro);

        var confetti = new ConfettiControl { Visible = false };
        ContentsContainer.AddChild(confetti);
        _confetti = confetti;
    }

    private readonly ConfettiControl _confetti;
    private IntroControl _intro;

    // ============================ UI construction ============================

    private Control BuildMainUi()
    {
        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 6,
        };
        scroll.AddChild(root);

        var header = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = SlotMachineUiColors.Gold,
                BorderColor = SlotMachineUiColors.GoldBorder,
                BorderThickness = new Thickness(2),
            },
        };
        var headerBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Align = BoxContainer.AlignMode.Center,
        };
        header.AddChild(headerBox);

        var titleRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Align = BoxContainer.AlignMode.Center,
            SeparationOverride = 8,
        };
        titleRow.AddChild(new TextureRect
        {
            Texture = _sevenIcon,
            Stretch = TextureRect.StretchMode.Scale,
            MinSize = new Vector2(16, 16),
        });
        var headerTitle = new Label
        {
            Text = Loc.GetString("slotmachine-casino-jackpot-header"),
            Align = Label.AlignMode.Center,
            FontColorOverride = SlotMachineUiColors.GoldDark,
        };
        headerTitle.FontOverride = _fonts.Get(16, bold: true);
        titleRow.AddChild(headerTitle);
        titleRow.AddChild(new TextureRect
        {
            Texture = _sevenIcon,
            Stretch = TextureRect.StretchMode.Scale,
            MinSize = new Vector2(16, 16),
        });
        headerBox.AddChild(titleRow);

        var amountRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Align = BoxContainer.AlignMode.Center,
            SeparationOverride = 6,
        };
        headerBox.AddChild(amountRow);
        _jackpotAmount.Text = Loc.GetString("slotmachine-casino-jackpot-amount", ("amount", 0));
        _jackpotAmount.FontColorOverride = Color.FromHex("#2d1000");
        _jackpotAmount.FontOverride = _fonts.Get(13, bold: true);
        amountRow.AddChild(_jackpotAmount);
        root.AddChild(header);

        root.AddChild(MakeBanner(_bonusBanner, SlotMachineUiColors.PinkBonus, 14));
        root.AddChild(MakeBanner(_streakBanner, Color.FromHex("#ff6633"), 12));

        var reelsPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#101024"),
                BorderColor = SlotMachineUiColors.PanelBorder,
                BorderThickness = new Thickness(2),
            },
        };
        reelsPanel.AddChild(_reels);
        reelsPanel.AddChild(_resultOverlay);
        root.AddChild(reelsPanel);


        root.AddChild(MakeBanner(_nearMissBanner, SlotMachineUiColors.OrangeHot, 13));
        root.AddChild(MakeBanner(_freeSpinsBanner, Color.FromHex("#4f4"), 13));
        root.AddChild(MakeBanner(_hotBanner, Color.FromHex("#ff6633"), 11));

        _gamblePanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#1a1a3e"),
            BorderColor = Color.FromHex("#ffd7004d"),
            BorderThickness = new Thickness(2),
        };
        var gambleBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 6,
        };
        _gamblePanel.AddChild(gambleBox);
        _gambleTitle.Align = Label.AlignMode.Center;
        _gambleTitle.FontColorOverride = SlotMachineUiColors.Gold;
        gambleBox.AddChild(_gambleTitle);

        var gambleButtons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };
        gambleBox.AddChild(gambleButtons);

        var redBtn = MakeActionButton(Loc.GetString("slotmachine-casino-gamble-red"), Color.FromHex("#c02020"));
        redBtn.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.Gamble, 0, "red");
        gambleButtons.AddChild(redBtn);

        var orLabel = new Label
        {
            Text = Loc.GetString("slotmachine-casino-or"),
            FontColorOverride = Color.Gray,
            Align = Label.AlignMode.Center,
        };
        gambleButtons.AddChild(orLabel);

        var blackBtn = MakeActionButton(Loc.GetString("slotmachine-casino-gamble-black"), Color.FromHex("#222222"));
        blackBtn.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.Gamble, 0, "black");
        gambleButtons.AddChild(blackBtn);

        var collectBtn = MakeActionButton(Loc.GetString("slotmachine-casino-gamble-collect"), Color.FromHex("#2f7a2f"));
        collectBtn.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.Collect);
        gambleButtons.AddChild(collectBtn);
        root.AddChild(_gamblePanel);

        var betSection = MakeSection(Loc.GetString("slotmachine-casino-bet-title"), out var betBox);
        var betRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 6,
        };
        betBox.AddChild(betRow);
        for (var i = 0; i < _betButtons.Length; i++)
        {
            var amount = new[] { 5, 10, 25, 50, 100 }[i];
            var lines = amount switch { 5 => 1, 10 => 2, _ => 3 };
            var btn = new Button { HorizontalExpand = true };
            var inner = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                Align = BoxContainer.AlignMode.Center,
            };
            var amountLabel = new Label
            {
                Text = Loc.GetString("slotmachine-casino-bet-amount", ("amount", amount)),
                Align = Label.AlignMode.Center,
                FontColorOverride = Color.White,
            };
            amountLabel.FontOverride = _fonts.Get(12, bold: true);
            var linesLabel = new Label
            {
                Text = Loc.GetString("slotmachine-casino-lines", ("count", lines)),
                Align = Label.AlignMode.Center,
                FontColorOverride = SlotMachineUiColors.GreyText,
            };
            linesLabel.FontOverride = _fonts.Get(9);
            inner.AddChild(amountLabel);
            inner.AddChild(linesLabel);
            btn.AddChild(inner);
            var captured = amount;
            btn.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.SetBet, captured);
            betRow.AddChild(btn);
            _betButtons[i] = btn;
        }
        root.AddChild(betSection);

        var controlsSection = MakeSection(string.Empty, out var controlsBox);
        var controlsRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 10,
        };
        controlsBox.AddChild(controlsRow);

        var balanceBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 1,
            SeparationOverride = 3,
        };
        balanceBox.AddChild(new Label { Text = Loc.GetString("slotmachine-casino-balance"), FontColorOverride = Color.Gray });
        _balanceLabel.Text = Loc.GetString("slotmachine-casino-balance-amount", ("amount", 0));
        _balanceLabel.FontColorOverride = SlotMachineUiColors.GreenWin;
        _balanceLabel.FontOverride = _fonts.Get(15, bold: true);
        balanceBox.AddChild(_balanceLabel);
        _sessionLabel.Visible = false;
        _sessionLabel.FontOverride = _fonts.Get(10);
        balanceBox.AddChild(_sessionLabel);
        balanceBox.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-paymode"),
            FontColorOverride = Color.Gray,
            FontOverride = _fonts.Get(9),
        });
        controlsRow.AddChild(balanceBox);

        var spinBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 1.2f,
            SeparationOverride = 3,
        };
        _spinButton.HorizontalExpand = true;
        _spinButton.TextAlign = Label.AlignMode.Center;
        _spinButton.Label!.FontOverride = _fonts.Get(15, bold: true);
        _spinButton.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#2f7a2f"),
            BorderColor = Color.FromHex("#1d4d1d"),
            BorderThickness = new Thickness(2),
            ContentMarginTopOverride = 8,
            ContentMarginBottomOverride = 8,
        };
        _spinButton.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.Spin);
        spinBox.AddChild(_spinButton);

        _autoRow.Align = BoxContainer.AlignMode.Center;
        foreach (var count in new[] { 5, 10, 25 })
        {
            var autoBtn = new Button
            {
                Text = $"x{count}",
                ToolTip = Loc.GetString("slotmachine-casino-autospin-tooltip", ("count", count)),
            };
            var captured = count;
            autoBtn.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.AutoSpin, captured);
            _autoRow.AddChild(autoBtn);
        }
        _stopAutoButton.Text = Loc.GetString("slotmachine-casino-stop-auto");
        _stopAutoButton.Visible = false;
        _stopAutoButton.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.StopAuto);
        _autoRow.AddChild(_stopAutoButton);
        spinBox.AddChild(_autoRow);

        _cashOutButton.Text = Loc.GetString("slotmachine-casino-cashout");
        _cashOutButton.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#8a7a20"),
            BorderColor = Color.FromHex("#554a10"),
            BorderThickness = new Thickness(1),
        };
        _cashOutButton.OnPressed += _ => _bui.SendAction(SlotMachineCasinoAction.CashOut);
        spinBox.AddChild(_cashOutButton);
        controlsRow.AddChild(spinBox);

        var statsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 1,
            SeparationOverride = 3,
        };
        statsBox.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-stats"),
            FontColorOverride = Color.Gray,
            Align = Label.AlignMode.Right,
        });
        _statsPlays.Align = Label.AlignMode.Right;
        statsBox.AddChild(_statsPlays);
        _statsJackpots.Align = Label.AlignMode.Right;
        statsBox.AddChild(_statsJackpots);
        _statsStreak.Align = Label.AlignMode.Right;
        _statsStreak.FontColorOverride = SlotMachineUiColors.GreenWin;
        statsBox.AddChild(_statsStreak);
        controlsRow.AddChild(statsBox);
        root.AddChild(controlsSection);

        var paytableSection = MakeSection(string.Empty, out var payBox);
        var payColumns = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 12,
        };
        payBox.AddChild(payColumns);

        var leftCol = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, SizeFlagsStretchRatio = 1 };
        leftCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-jackpot-symbols"), Loc.GetString("slotmachine-casino-pay-jackpot-prize"), SlotMachineUiColors.Gold));
        leftCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-five-symbols"), string.Empty, SlotMachineUiColors.BlueText, _paytableFive));
        leftCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-four-symbols"), string.Empty, SlotMachineUiColors.BlueText, _paytableFour));
        leftCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-three-symbols"), Loc.GetString("slotmachine-casino-pay-three-prize"), SlotMachineUiColors.BlueText));
        payColumns.AddChild(leftCol);

        var rightCol = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, SizeFlagsStretchRatio = 1 };
        rightCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-wild-symbols"), Loc.GetString("slotmachine-casino-pay-wild-prize"), Color.FromHex("#88ff88")));
        rightCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-scatter-symbols"), Loc.GetString("slotmachine-casino-pay-scatter-prize"), SlotMachineUiColors.PinkBonus));
        rightCol.AddChild(PayRow(Loc.GetString("slotmachine-casino-pay-bonus-symbols"), Loc.GetString("slotmachine-casino-pay-bonus-prize"), SlotMachineUiColors.PinkBonus));
        rightCol.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-pay-note"),
            FontColorOverride = Color.Gray,
            FontOverride = _fonts.Get(9),
        });
        rightCol.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-pay-lines-legend"),
            FontColorOverride = Color.Gray,
            FontOverride = _fonts.Get(9),
        });
        payColumns.AddChild(rightCol);
        root.AddChild(paytableSection);

        return scroll;
    }

    private IntroControl BuildIntro()
    {
        var intro = new IntroControl(GetRsiTexture(MachineRsiPath, "mahins"))
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var layout = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Align = BoxContainer.AlignMode.Center,
            SeparationOverride = 8,
        };
        intro.AddChild(layout);
        layout.AddChild(new Control { VerticalExpand = true });

        var title = new Label
        {
            Text = Loc.GetString("slotmachine-casino-intro-title"),
            Align = Label.AlignMode.Center,
            FontColorOverride = SlotMachineUiColors.Gold,
        };
        title.FontOverride = _fonts.Get(34, bold: true);
        layout.AddChild(title);

        layout.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-intro-subtitle"),
            Align = Label.AlignMode.Center,
            FontColorOverride = Color.FromHex("#8080cc"),
            FontOverride = _fonts.ItalicFont(13),
        });

        var symbolsRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Align = BoxContainer.AlignMode.Center,
        };
        foreach (var sym in SlotMachineCasinoComponent.SymbolIds)
        {
            symbolsRow.AddChild(new TextureRect
            {
                Texture = _reels.Symbols.GetValueOrDefault(sym),
                Stretch = TextureRect.StretchMode.Scale,
                MinSize = new Vector2(26, 26),
            });
        }
        layout.AddChild(symbolsRow);

        var click = new Label
        {
            Text = Loc.GetString("slotmachine-casino-intro-click"),
            Align = Label.AlignMode.Center,
            FontColorOverride = SlotMachineUiColors.BlueText,
        };
        click.FontOverride = _fonts.Get(14, bold: true);
        intro.ClickLabel = click;
        layout.AddChild(click);
        layout.AddChild(new Control { VerticalExpand = true });

        intro.OnKeyBindDown += args =>
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                intro.Visible = false;
                args.Handle();
            }
        };

        return intro;
    }

    // ============================ helpers ============================

    private PanelContainer MakeBanner(Label label, Color textColor, int fontSize)
    {
        label.Align = Label.AlignMode.Center;
        label.FontColorOverride = textColor;
        label.FontOverride = _fonts.Get(fontSize, bold: true);
        label.Visible = false;
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            Visible = false,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = textColor * new Color(1, 1, 1, 0.1f),
                BorderColor = textColor * new Color(1, 1, 1, 0.4f),
                BorderThickness = new Thickness(1),
            },
        };
        panel.AddChild(label);
        return panel;
    }

    private PanelContainer MakeSection(string title, out BoxContainer box)
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#1a1a30"),
                BorderColor = Color.FromHex("#2a2a4a"),
                BorderThickness = new Thickness(1),
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6,
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
            },
        };
        box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };
        panel.AddChild(box);

        if (!string.IsNullOrEmpty(title))
        {
            box.AddChild(new Label
            {
                Text = title,
                FontColorOverride = Color.White,
                FontOverride = _fonts.Get(12, bold: true),
            });
        }

        return panel;
    }

    private BoxContainer PayRow(string symbols, string prize, Color color, Label? prizeLabel = null)
    {
        var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
        row.AddChild(new Label
        {
            Text = symbols,
            FontColorOverride = SlotMachineUiColors.GreyText,
            HorizontalExpand = true,
        });
        var label = prizeLabel ?? new Label();
        label.Text = prize;
        label.FontColorOverride = color;
        label.FontOverride = _fonts.Get(11, bold: true);
        row.AddChild(label);
        return row;
    }

    private Button MakeActionButton(string text, Color bg)
    {
        var btn = new Button
        {
            Text = text,
            HorizontalExpand = true,
            TextAlign = Label.AlignMode.Center,
        };
        btn.Label!.FontOverride = _fonts.Get(12, bold: true);
        btn.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = bg,
            BorderColor = bg * new Color(1, 1, 1, 0.6f),
            BorderThickness = new Thickness(1),
            ContentMarginTopOverride = 5,
            ContentMarginBottomOverride = 5,
        };
        return btn;
    }

    // ============================ state ============================

    public void UpdateState(SlotMachineCasinoBoundUserInterfaceState state)
    {
        _state = state;

        _jackpotAmount.Text = Loc.GetString("slotmachine-casino-jackpot-amount", ("amount", state.Money));
        _reels.Reels = state.Reels;
        _reels.Stopped = state.ReelsStopped;
        _reels.Working = state.Working;
        _reels.WinningLine = state.WinningLine;

        _bonusBanner.Visible = state.BonusActive && !state.Working;
        _bonusBanner.Text = Loc.GetString("slotmachine-casino-banner-bonus", ("multiplier", state.BonusMultiplier));
        _streakBanner.Visible = state.WinStreak >= 2;
        _streakBanner.Text = Loc.GetString("slotmachine-casino-banner-streak", ("count", state.WinStreak));

        if (_lastSpinCount == -1)
        {
            _lastSpinCount = state.SpinCount;
        }
        else if (state.SpinCount != _lastSpinCount && !state.Working &&
                 !string.IsNullOrEmpty(state.ResultMessage))
        {
            _lastSpinCount = state.SpinCount;
            _pendingResult = (
                state.ResultMessage,
                ResultColor(state.ResultType),
                state.ResultType == "jackpot" ? 22 : 18);
        }


        _nearMissBanner.Visible = state.NearMiss && !state.Working;
        _nearMissBanner.Text = Loc.GetString("slotmachine-casino-nearmiss", ("message", state.NearMissMessage));
        _freeSpinsBanner.Visible = state.FreeSpinsRemaining > 0;
        _freeSpinsBanner.Text = Loc.GetString("slotmachine-casino-banner-freespins", ("count", state.FreeSpinsRemaining));
        _hotBanner.Visible = state.LossStreak >= 5 && !state.Working;
        _hotBanner.Text = Loc.GetString("slotmachine-casino-banner-hot", ("count", state.LossStreak));

        _gambleTitle.Text = Loc.GetString("slotmachine-casino-gamble-title", ("amount", state.GambleAmount));

        var betMultiplier = state.BetAmount / Math.Max(1, state.SpinPriceMin);
        if (_lastBet != state.BetAmount)
        {
            _lastBet = state.BetAmount;
            for (var i = 0; i < _betButtons.Length; i++)
            {
                var bet = new[] { 5, 10, 25, 50, 100 }[i];
                var selected = state.BetAmount == bet;
                _betButtons[i].StyleBoxOverride = selected
                    ? _selectedBetStyle
                    : _defaultBetStyle;
            }
        }

        foreach (var btn in _betButtons)
            btn.Disabled = state.Working;

        _paytableFive.Text = Loc.GetString("slotmachine-casino-bet-amount", ("amount", state.BigPrizeBase * betMultiplier));
        _paytableFour.Text = Loc.GetString("slotmachine-casino-bet-amount", ("amount", state.SmallPrizeBase * betMultiplier));

        var canSpin = state.FreeSpinsRemaining > 0 || state.Balance >= state.BetAmount;
        _balanceLabel.Text = Loc.GetString("slotmachine-casino-balance-amount", ("amount", state.Balance));
        if (state.SessionWinnings != 0)
        {
            _sessionLabel.Visible = true;
            _sessionLabel.Text = Loc.GetString("slotmachine-casino-session",
                ("sign", state.SessionWinnings > 0 ? "+" : ""), ("amount", state.SessionWinnings));
            _sessionLabel.FontColorOverride = state.SessionWinnings > 0 ? SlotMachineUiColors.GreenWin : SlotMachineUiColors.LineRed;
        }
        else
        {
            _sessionLabel.Visible = false;
        }

        _spinButton.Text = state.Working
            ? (state.AutoSpin ? Loc.GetString("slotmachine-casino-spin-auto") : Loc.GetString("slotmachine-casino-spin-spinning"))
            : state.FreeSpinsRemaining > 0
                ? Loc.GetString("slotmachine-casino-spin-free")
                : Loc.GetString("slotmachine-casino-spin", ("amount", state.BetAmount));

        _stopAutoButton.Visible = state.AutoSpin;
        foreach (var child in _autoRow.Children)
        {
            if (child is Button b && b != _stopAutoButton)
                b.Visible = !state.AutoSpin;
        }

        _statsPlays.Text = Loc.GetString("slotmachine-casino-stats-plays", ("count", state.Plays));
        _statsJackpots.Text = Loc.GetString("slotmachine-casino-stats-jackpots", ("count", state.Jackpots));
        _statsStreak.Text = state.WinStreak > 0
            ? Loc.GetString("slotmachine-casino-stats-streak", ("count", state.WinStreak))
            : string.Empty;
        _statsStreak.Visible = state.WinStreak > 0;

        _confetti.Visible = state.ShowConfetti && !state.Working;
        if (_confetti.Visible)
            _confetti.Restart();
    }

    private readonly StyleBoxFlat _selectedBetStyle = new()
    {
        BackgroundColor = Color.FromHex("#2f7a2f"),
        BorderColor = Color.FromHex("#1d4d1d"),
        BorderThickness = new Thickness(2),
    };

    private readonly StyleBoxFlat _defaultBetStyle = new()
    {
        BackgroundColor = Color.FromHex("#252550"),
        BorderColor = Color.FromHex("#333366"),
        BorderThickness = new Thickness(1),
    };



    private static Color ResultColor(string type) => type switch
    {
        "jackpot" => SlotMachineUiColors.Gold,
        "win" => SlotMachineUiColors.GreenWin,
        "small" => SlotMachineUiColors.BlueText,
        "bonus" => SlotMachineUiColors.PinkBonus,
        _ => SlotMachineUiColors.GreyLose,
    };

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _time += args.DeltaSeconds;
        _reels.Time = _time;
        _intro.Time = _time;

        if (_state == null)
            return;

        var uiReady = !_state.Working && !_reels.Working && _reels.AllLanded;
        _gamblePanel.Visible = uiReady && _state.GambleAvailable;

        var canSpin = uiReady &&
            (_state.FreeSpinsRemaining > 0 || _state.Balance >= _state.BetAmount);
        _spinButton.Disabled = !canSpin;
        _cashOutButton.Visible = uiReady && _state.Balance > 0 && !_state.AutoSpin;
        foreach (var child in _autoRow.Children)
        {
            if (child is Button b && b != _stopAutoButton)
                b.Disabled = !uiReady;
        }

        if (_state.Working)
        {
            _pendingResult = null;
            _resultOverlay.Hide();
        }
        else if (_pendingResult != null && uiReady)
        {
            var (text, color, size) = _pendingResult.Value;
            _resultOverlay.Show(text, color, _fonts.Get(size, bold: true));
            _pendingResult = null;
        }
    }

    private float _time;


    private const string SymbolsRsiPath = "/Textures/ADT/Structures/Machines/slotmachines/slot_symbols.rsi";
    private const string MachineRsiPath = "/Textures/ADT/Structures/Machines/slotmachines/kazino.rsi";
    private Texture? GetRsiTexture(string rsiPath, string state)
    {
        var rsi = _resCache.GetResource<RSIResource>(new ResPath(rsiPath)).RSI;
        if (!rsi.TryGetState(state, out var frames))
            return null;
        var textures = frames.GetFrames(RsiDirection.South);
        return textures.Length > 0 ? textures[0] : null;
    }
}
