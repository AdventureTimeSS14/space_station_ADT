// SPDX-FileCopyrightText: 2025 LocalDuty
//
// SPDX-License-Identifier: MIT

using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Stylesheets;

/// <summary>
///     Стили для кнопок левой панели лобби LocalDuty.
///     Шрифт: Montserrat Alternates
///     Обычное состояние: #4E5754 (серый)
///     Hover: #FFFAFA (почти белый) + отступ слева +8px
/// </summary>
public static class StyleLobbyDuty
{
    public const string LobbyButtonDuty = "LobbyButtonDuty";

    public static StyleRule[] GetRules(IResourceCache resCache)
    {
        var montserrat20 = resCache.GetFont(
            "/Fonts/Duty/texts/Montserrat Alternates.ttf",
            20
        );

        // Обычное состояние — без фона, отступ слева 0
        var boxNormal = new StyleBoxFlat
        {
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent,
        };
        boxNormal.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);
        boxNormal.SetContentMarginOverride(StyleBox.Margin.Left, 0);
        boxNormal.SetContentMarginOverride(StyleBox.Margin.Right, 0);

        // Hover состояние — тот же прозрачный фон, но отступ слева +8px для сдвига
        var boxHover = new StyleBoxFlat
        {
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent,
        };
        boxHover.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);
        boxHover.SetContentMarginOverride(StyleBox.Margin.Left, 8);
        boxHover.SetContentMarginOverride(StyleBox.Margin.Right, 0);

        var colorNormal   = Color.FromHex("#4E5754");
        var colorHover    = Color.FromHex("#FFFAFA");
        var colorPressed  = Color.FromHex("#CCCCCC");
        var colorDisabled = Color.FromHex("#333333");

        return new StyleRule[]
        {
            // ── Обычное состояние ──────────────────────────────────────────
            new StyleRule(
                new SelectorElement(typeof(Button), new[] { LobbyButtonDuty }, null, null),
                new[]
                {
                    new StyleProperty(Button.StylePropertyStyleBox, boxNormal),
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Button.StylePropertyModulateSelf, Color.White),
                }),

            // Label обычный
            new StyleRule(
                new SelectorElement(typeof(Label), new[] { Button.StyleClassButton },
                    LobbyButtonDuty, null),
                new[]
                {
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Label.StylePropertyFontColor, colorNormal),
                }),

            // ── Hover ──────────────────────────────────────────────────────
            new StyleRule(
                new SelectorElement(typeof(Button), new[] { LobbyButtonDuty }, null,
                    new[] { Button.StylePseudoClassHover }),
                new[]
                {
                    new StyleProperty(Button.StylePropertyStyleBox, boxHover),
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Button.StylePropertyModulateSelf, Color.White),
                }),

            // Label hover
            new StyleRule(
                new SelectorElement(typeof(Label), new[] { Button.StyleClassButton },
                    LobbyButtonDuty, new[] { Button.StylePseudoClassHover }),
                new[]
                {
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Label.StylePropertyFontColor, colorHover),
                }),

            // ── Pressed ────────────────────────────────────────────────────
            new StyleRule(
                new SelectorElement(typeof(Button), new[] { LobbyButtonDuty }, null,
                    new[] { Button.StylePseudoClassPressed }),
                new[]
                {
                    new StyleProperty(Button.StylePropertyStyleBox, boxHover),
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Button.StylePropertyModulateSelf, Color.White),
                }),

            new StyleRule(
                new SelectorElement(typeof(Label), new[] { Button.StyleClassButton },
                    LobbyButtonDuty, new[] { Button.StylePseudoClassPressed }),
                new[]
                {
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Label.StylePropertyFontColor, colorPressed),
                }),

            // ── Disabled ───────────────────────────────────────────────────
            new StyleRule(
                new SelectorElement(typeof(Button), new[] { LobbyButtonDuty }, null,
                    new[] { Button.StylePseudoClassDisabled }),
                new[]
                {
                    new StyleProperty(Button.StylePropertyStyleBox, boxNormal),
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Button.StylePropertyModulateSelf, Color.White),
                }),

            new StyleRule(
                new SelectorElement(typeof(Label), new[] { Button.StyleClassButton },
                    LobbyButtonDuty, new[] { Button.StylePseudoClassDisabled }),
                new[]
                {
                    new StyleProperty("font", montserrat20),
                    new StyleProperty(Label.StylePropertyFontColor, colorDisabled),
                }),
        };
    }
}
