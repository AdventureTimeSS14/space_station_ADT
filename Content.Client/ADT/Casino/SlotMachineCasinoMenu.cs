using System.Numerics;
using System.Linq;
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
    private readonly Button[] _betButtons = new Button[5];
    private readonly PanelContainer _gamblePanel = new() { Visible = false };
    private readonly Label _gambleTitle = new();
    private readonly Control _mainContent;
    private int _lastBet = -1;
    private readonly ResultOverlayControl _resultOverlay = new();
    private SlotMachineCasinoBoundUserInterfaceState? _state;
    private int _lastSpinCount = -1;
    private (string Text, Color Color, int Size)? _pendingResult;

    private readonly PanelContainer _agreementPanel;
    private readonly BoxContainer _oddsList = new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 1 };
    private readonly Dictionary<string, Label> _oddsLabels = new();
    private readonly TextureButton _rulesButton = new();
    private Dictionary<string, int>? _lastWeights;
    private bool _agreementOpen;
    private readonly Dictionary<Label, (string Text, int Size, int Width, bool Bold)> _fitCache = new();
    private readonly Dictionary<Label, int> _fitLines = new();

    private const string SymbolsRsiPath = "/Textures/ADT/Structures/Machines/slotmachines/casino_symbols.rsi";

    private static readonly (string Id, string State, Color TextColor)[] SymbolData =
    {
        ("seven", "seven", SlotMachineUiColors.Gold),
        ("cherry", "cherry", SlotMachineUiColors.LineRed),
        ("banana", "banana", Color.FromHex("#ffe066")),
        ("strawberry", "strawberry", Color.FromHex("#ff8899")),
        ("grape", "grape", Color.FromHex("#c090ff")),
        ("watermelon", "watermelon", SlotMachineUiColors.LineGreen),
        ("diamond", "diamond", SlotMachineUiColors.BlueText),
        ("star", "star", SlotMachineUiColors.OrangeHot),
        ("wild", "wild", SlotMachineUiColors.GreenWin),
        ("scatter", "scatter", SlotMachineUiColors.PinkBonus),
    };

    private readonly Texture? _sevenIcon;

    public SlotMachineCasinoMenu(SlotMachineCasinoBoundUserInterface bui)
    {
        _bui = bui;
        IoCManager.InjectDependencies(this);
        _resCache = IoCManager.Resolve<IResourceCache>();
        _fonts = new SlotMachineFonts(_resCache);

        Title = Loc.GetString("slotmachine-casino-title");
        SetSize = new Vector2(540, 545);

        foreach (var (id, state, _) in SymbolData)
        {
            if (GetRsiTexture(SymbolsRsiPath, state) is { } tex)
                _reels.Symbols[id] = tex;
        }

        _sevenIcon = _reels.Symbols.GetValueOrDefault("seven");

        AddRulesButton();

        _mainContent = BuildMainUi();
        _agreementPanel = BuildAgreement();

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 8,
        };
        root.AddChild(_mainContent);
        root.AddChild(_agreementPanel);
        ContentsContainer.AddChild(root);

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
        var headerTitle = new Label
        {
            Text = Loc.GetString("slotmachine-casino-jackpot-header"),
            Align = Label.AlignMode.Center,
            FontColorOverride = SlotMachineUiColors.GoldDark,
        };
        headerTitle.FontOverride = _fonts.Get(16, bold: true);
        titleRow.AddChild(headerTitle);
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
                Stretch = TextureRect.StretchMode.KeepAspect,
                MinSize = new Vector2(32, 32),
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

    private void AddRulesButton()
    {
        var texture = _resCache.GetResource<TextureResource>("/Textures/Interface/VerbIcons/information.svg.192dpi.png").Texture;
        _rulesButton.TextureNormal = texture;
        _rulesButton.ToolTip = Loc.GetString("slotmachine-casino-agreement-tooltip");
        _rulesButton.VerticalAlignment = Control.VAlignment.Center;
        _rulesButton.SetSize = new Vector2(16, 16);
        _rulesButton.Margin = new Thickness(0, 0, 4, 0);
        _rulesButton.OnPressed += _ => ToggleAgreement();

        var header = (Control) CloseButton.Parent!;
        header.RemoveChild(CloseButton);
        header.AddChild(_rulesButton);
        header.AddChild(CloseButton);
    }

    private void ToggleAgreement()
    {
        _agreementOpen = !_agreementOpen;
        _agreementPanel.Visible = _agreementOpen;
    }

    private PanelContainer BuildAgreement()
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Visible = false,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#161629"),
                BorderColor = Color.FromHex("#ffd70066"),
                BorderThickness = new Thickness(2),
                ContentMarginTopOverride = 10,
                ContentMarginBottomOverride = 10,
                ContentMarginLeftOverride = 12,
                ContentMarginRightOverride = 12,
            },
        };

        var layout = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };
        panel.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-agreement-company"),
            Align = Label.AlignMode.Center,
            FontColorOverride = Color.FromHex("#8a8aa0"),
            FontOverride = _fonts.Get(10),
        });
        layout.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-agreement-title"),
            Align = Label.AlignMode.Center,
            FontColorOverride = SlotMachineUiColors.Gold,
            FontOverride = _fonts.Get(15, bold: true),
        });
        layout.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-agreement-subtitle"),
            Align = Label.AlignMode.Center,
            FontColorOverride = Color.FromHex("#8080cc"),
            FontOverride = _fonts.ItalicFont(10),
        });

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
        };
        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };
        scroll.AddChild(body);
        layout.AddChild(scroll);

        AgreementSection(body, SlotMachineUiColors.LineRed, "slotmachine-casino-agreement-notice-header",
            new[] { "slotmachine-casino-agreement-notice" });
        AgreementSection(body, SlotMachineUiColors.Gold, "slotmachine-casino-agreement-howto-header",
            new[] { "slotmachine-casino-agreement-howto" });
        AgreementSection(body, SlotMachineUiColors.GreenWin, "slotmachine-casino-agreement-payout-header",
            new[]
            {
                "slotmachine-casino-agreement-payout-5",
                "slotmachine-casino-agreement-payout-4",
                "slotmachine-casino-agreement-payout-3",
                "slotmachine-casino-agreement-payout-scatter",
                "slotmachine-casino-agreement-payout-bonus",
                "slotmachine-casino-agreement-payout-gamble",
                "slotmachine-casino-agreement-payout-jackpot",
            });

        body.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-agreement-odds-header"),
            FontColorOverride = SlotMachineUiColors.BlueText,
            FontOverride = _fonts.Get(12, bold: true),
        });
        foreach (var sym in SlotMachineCasinoComponent.SymbolIds)
        {
            var label = new Label { FontOverride = _fonts.Get(11) };
            _oddsLabels[sym] = label;
            _oddsList.AddChild(label);
        }
        body.AddChild(_oddsList);
        body.AddChild(new Label
        {
            Text = Loc.GetString("slotmachine-casino-agreement-odds-note"),
            FontColorOverride = Color.FromHex("#8a8aa0"),
            FontOverride = _fonts.Get(9),
        });

        return panel;
    }

    private void AgreementSection(
        BoxContainer body, Color headerColor, string header,
        string[] lines, bool fine = false)
    {
        body.AddChild(new Label
        {
            Text = Loc.GetString(header),
            FontColorOverride = headerColor,
            FontOverride = _fonts.Get(12, bold: true),
        });
        foreach (var line in lines)
        {
            // RichTextLabel wraps text to the panel width; plain Labels would overflow and get clipped.
            var rtl = new RichTextLabel { HorizontalExpand = true };
            rtl.SetMessage(
                FormattedMessage.FromMarkupPermissive(Loc.GetString(line)),
                fine ? Color.FromHex("#9a9aa8") : Color.FromHex("#c8c8d8"));
            body.AddChild(rtl);
        }
    }

    private void UpdateOdds(Dictionary<string, int> weights)
    {
        if (_lastWeights != null && _lastWeights.Count == weights.Count &&
            _lastWeights.All(kv => weights.TryGetValue(kv.Key, out var w) && w == kv.Value))
            return;
        _lastWeights = new Dictionary<string, int>(weights);

        var total = 0;
        foreach (var (_, weight) in weights)
            total += weight;

        foreach (var (id, _, color) in SymbolData)
        {
            if (!_oddsLabels.TryGetValue(id, out var label))
                continue;
            var weight = weights.GetValueOrDefault(id);
            var percent = total > 0 ? (int) MathF.Round(weight * 100f / total) : 0;
            label.Text = Loc.GetString("slotmachine-casino-agreement-odds-line",
                ("name", Loc.GetString($"slotmachine-casino-sym-{id}")), ("percent", percent));
            label.FontColorOverride = color;
        }
    }

    // ============================ helpers ============================

    private float BannerMaxWidth() => MathF.Max(160f, Size.X - 44f);

    private int BannerLines(Label label) => _fitLines.GetValueOrDefault(label, 1);

    private static float MeasureText(Font font, string text)
    {
        var width = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            var metrics = font.GetCharMetrics(rune, 1f);
            if (metrics != null)
                width += metrics.Value.Advance;
        }
        return width;
    }

    private static List<string> WrapLines(Font font, string text, float maxWidth)
    {
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in text.Split(' '))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && MeasureText(font, candidate) > maxWidth)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }
        if (current.Length > 0)
            lines.Add(current);
        return lines;
    }

    private int SetFittedText(Label label, string text, int size, bool bold, float maxWidth, int maxLines = 2)
    {
        var key = (text, size, (int) maxWidth, bold);
        if (_fitCache.TryGetValue(label, out var cached) && cached == key)
            return _fitLines.GetValueOrDefault(label, 1);
        _fitCache[label] = key;

        for (var s = size; s >= 9; s--)
        {
            var font = _fonts.Get(s, bold);
            var lines = WrapLines(font, text, maxWidth);
            if (lines.Count <= maxLines)
            {
                label.FontOverride = font;
                label.Text = string.Join('\n', lines);
                _fitLines[label] = lines.Count;
                return lines.Count;
            }
        }

        var fallback = _fonts.Get(9, bold);
        var fallbackLines = WrapLines(fallback, text, maxWidth);
        label.FontOverride = fallback;
        label.Text = string.Join('\n', fallbackLines);
        _fitLines[label] = fallbackLines.Count;
        return fallbackLines.Count;
    }

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
        SetFittedText(_bonusBanner, Loc.GetString("slotmachine-casino-banner-bonus", ("multiplier", state.BonusMultiplier)), 14, true, BannerMaxWidth());
        _streakBanner.Visible = state.WinStreak >= 2;
        SetFittedText(_streakBanner, Loc.GetString("slotmachine-casino-banner-streak", ("count", state.WinStreak)), 12, true, BannerMaxWidth());

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
        SetFittedText(_nearMissBanner, Loc.GetString("slotmachine-casino-nearmiss", ("message", state.NearMissMessage)), 13, true, BannerMaxWidth());
        _freeSpinsBanner.Visible = state.FreeSpinsRemaining > 0;
        SetFittedText(_freeSpinsBanner, Loc.GetString("slotmachine-casino-banner-freespins", ("count", state.FreeSpinsRemaining)), 13, true, BannerMaxWidth());
        _hotBanner.Visible = state.LossStreak >= state.LossStreakHot && !state.Working;
        SetFittedText(_hotBanner, Loc.GetString("slotmachine-casino-banner-hot", ("count", state.LossStreak)), 11, true, BannerMaxWidth());

        _gambleTitle.Text = Loc.GetString("slotmachine-casino-gamble-title", ("amount", state.GambleAmount));

        UpdateOdds(state.SymbolWeights);

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
            btn.Disabled = state.Working || state.FreeSpinsRemaining > 0;

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
        _cashOutButton.Disabled = !uiReady || _state.Balance <= 0;
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
            SetFittedText(_resultOverlay.Label, text, size, true,
                MathF.Max(220f, _reels.Width - 16f), maxLines: 3);
            _resultOverlay.Show(_resultOverlay.Label.Text ?? text, color, _resultOverlay.Label.FontOverride!);
            _pendingResult = null;
        }

        var extra = 0f;
        if (_bonusBanner.Visible)
            extra += 6 + 20 * BannerLines(_bonusBanner);
        if (_streakBanner.Visible)
            extra += 6 + 20 * BannerLines(_streakBanner);
        if (_nearMissBanner.Visible)
            extra += 6 + 20 * BannerLines(_nearMissBanner);
        if (_freeSpinsBanner.Visible)
            extra += 6 + 20 * BannerLines(_freeSpinsBanner);
        if (_hotBanner.Visible)
            extra += 6 + 20 * BannerLines(_hotBanner);
        if (_gamblePanel.Visible)
            extra += 74;

        var targetW = _agreementOpen ? 980f : 540f;
        var targetH = 545f + extra;
        if (MathF.Abs(Size.X - targetW) > 1f || MathF.Abs(Size.Y - targetH) > 1f)
        {
            var center = Position + Size / 2f;
            SetSize = new Vector2(targetW, targetH);
            LayoutContainer.SetPosition(this, center - Size / 2f);
        }
    }

    private float _time;


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
