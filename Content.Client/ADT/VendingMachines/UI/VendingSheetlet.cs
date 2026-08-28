using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.ADT.VendingMachines.UI;

[CommonSheetlet]
public sealed class VendingSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        // Color palette
        var bgDark = Color.FromHex("#1a1a22");
        var bgMedium = Color.FromHex("#22222a");
        var bgLight = Color.FromHex("#2a2a35");
        var bgLighter = Color.FromHex("#32323e");
        var textPrimary = Color.FromHex("#e0e0e0");
        var textSecondary = Color.FromHex("#a0a0a0");
        var textMuted = Color.FromHex("#707070");
        var accentBlue = Color.FromHex("#60a5fa");
        var accentGreen = Color.FromHex("#4ade80");
        var accentYellow = Color.FromHex("#fbbf24");

        // StyleBoxes
        var headerPanelBox = new StyleBoxFlat
        {
            BackgroundColor = bgLight,
            BorderColor = bgLighter,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        headerPanelBox.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var headerAccentBox = new StyleBoxFlat { BackgroundColor = accentBlue };

        var searchBarBox = new StyleBoxFlat { BackgroundColor = bgMedium };
        searchBarBox.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var searchInputBox = new StyleBoxFlat
        {
            BackgroundColor = bgDark,
            ContentMarginLeftOverride = 8,
            ContentMarginRightOverride = 8
        };

        var categoryBarBox = new StyleBoxFlat
        {
            BackgroundColor = bgMedium,
            BorderColor = bgLighter,
            BorderThickness = new Thickness(0, 0, 1, 0)
        };
        categoryBarBox.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var entryPanelBox = new StyleBoxFlat
        {
            BackgroundColor = bgLight,
            BorderColor = bgLighter,
            BorderThickness = new Thickness(1)
        };
        entryPanelBox.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var entryDisabledBox = new StyleBoxFlat
        {
            BackgroundColor = bgDark,
            BorderColor = Color.FromHex("#2a2a2a"),
            BorderThickness = new Thickness(1)
        };
        entryDisabledBox.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var footerPanelBox = new StyleBoxFlat
        {
            BackgroundColor = bgMedium,
            BorderColor = bgLighter,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        var rules = new List<StyleRule>
        {
            // ===== HEADER PANEL =====
            E<PanelContainer>()
                .Class("VendingHeaderPanel")
                .Panel(headerPanelBox),

            E<PanelContainer>()
                .Class("VendingHeaderAccent")
                .Panel(headerAccentBox),

            E<Label>()
                .Class("VendingBalanceLabel")
                .Font(sheet.BaseFont.GetFont(14))
                .FontColor(accentGreen),

            E<Label>()
                .Class("VendingCreditsLabel")
                .Font(sheet.BaseFont.GetFont(13))
                .FontColor(accentYellow),

            // ===== SEARCH BAR =====
            E<PanelContainer>()
                .Class("VendingSearchBar")
                .Panel(searchBarBox),

            E<LineEdit>()
                .Class("VendingSearchInput")
                .Panel(searchInputBox),

            // ===== CATEGORY BAR =====
            E<PanelContainer>()
                .Class("VendingCategoryBar")
                .Panel(categoryBarBox),

            // ===== ENTRY =====
            E<PanelContainer>()
                .Class("VendingEntryPanel")
                .Panel(entryPanelBox),

            E<PanelContainer>()
                .Class("VendingEntryPanel", "VendingEntryDisabled")
                .Panel(entryDisabledBox)
                .Modulate(new Color(1f, 1f, 1f, 0.5f)),

            E<RichTextLabel>()
                .Class("VendingEntryNameLabel")
                .Font(sheet.BaseFont.GetFont(11))
                .FontColor(textPrimary),

            E<RichTextLabel>()
                .Class("VendingEntryCountLabel")
                .Font(sheet.BaseFont.GetFont(11))
                .FontColor(textSecondary),

            E<RichTextLabel>()
                .Class("VendingEntryPriceLabel")
                .Font(sheet.BaseFont.GetFont(11)),

            // ===== FOOTER =====
            E<PanelContainer>()
                .Class("VendingFooterPanel")
                .Panel(footerPanelBox),

            E<Label>()
                .Class("VendingFooterText")
                .Font(sheet.BaseFont.GetFont(12))
                .FontColor(textMuted),

            E<Label>()
                .Class("VendingEmptyLabel")
                .Font(sheet.BaseFont.GetFont(14))
                .FontColor(textMuted),
        };

        return rules.ToArray();
    }
}
