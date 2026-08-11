using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.PDA;

public readonly record struct PdaInteriorPalette(
    Color ScanLight,
    Color ScanDark,
    Color Chrome,
    Color NavInactive,
    Color NavActive,
    Color NavBorder,
    Color Fg,
    Color FgMuted,
    Color ItemBg,
    Color ItemHover,
    Color Divider,
    Color FooterFg,
    Color FooterStripe,
    Color PanelBg,
    Color ButtonBorder);

public static class PdaInteriorTheme
{
    /// <summary>
    /// Ink for text on the outer PDA plastic: white on dark frames, dark on light frames.
    /// </summary>
    public static Color ContrastingInk(Color surface)
    {
        return Color.ToHsv(surface).Z < 0.55f
            ? Color.White
            : Color.FromHex("#1A1A1A");
    }

    public static PdaInteriorPalette FromSecondary(Color secondary)
    {
        var hsv = Color.ToHsv(secondary);
        var h = hsv.X;
        var s = Math.Clamp(Math.Max(hsv.Y, 0.12f), 0.12f, 0.90f);

        const float scanDarkV = 0.10f;
        const float scanLightV = 0.16f;

        return new PdaInteriorPalette(
            ScanLight: Color.FromHsv(new Vector4(h, s * 0.50f, scanLightV, 1f)),
            ScanDark: Color.FromHsv(new Vector4(h, s * 0.55f, scanDarkV, 1f)),
            Chrome: Color.FromHsv(new Vector4(h, s * 0.25f, 0.55f, 1f)),
            NavInactive: Color.FromHsv(new Vector4(h, s * 0.40f, 0.22f, 1f)),
            NavActive: Color.FromHsv(new Vector4(h, s * 0.40f, 0.30f, 1f)),
            NavBorder: Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.55f, 0.25f, 0.70f), 0.72f, 1f)),
            Fg: Color.FromHsv(new Vector4(h, s * 0.15f, 0.95f, 1f)),
            FgMuted: Color.FromHsv(new Vector4(h, s * 0.20f, 0.55f, 1f)),
            ItemBg: Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.45f, 0.18f, 0.65f), 0.28f, 1f)),
            ItemHover: Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.75f, 0.35f, 0.85f), 0.48f, 1f)),
            Divider: Color.FromHsv(new Vector4(h, s * 0.40f, 0.30f, 1f)),
            FooterFg: Color.FromHsv(new Vector4(h, s * 0.15f, 0.96f, 1f)),
            FooterStripe: Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.35f, 0.08f, 0.40f), 0.96f, 1f)),
            PanelBg: Color.FromHsv(new Vector4(h, s * 0.40f, 0.14f, 0.90f)),
            ButtonBorder: Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.55f, 0.25f, 0.70f), 0.72f, 1f))
        );
    }

    public static void ApplyTo(Control root, in PdaInteriorPalette palette)
    {
        ApplyRecursive(root, palette);
    }

    private static void ApplyRecursive(Control control, in PdaInteriorPalette palette)
    {
        switch (control)
        {
            case PanelContainer panel:
                ThemePanel(panel, palette);
                break;
            case Label label:
                ThemeLabel(label, palette);
                break;
            case RichTextLabel rich:
                if (rich.ModulateSelfOverride is { } mod && IsDark(mod))
                    rich.ModulateSelfOverride = null;
                break;
            case Button button:
                ThemeButton(button, palette);
                break;
            case LineEdit lineEdit:
                ThemeLineEdit(lineEdit, palette);
                break;
            case StripeBack stripe:
                stripe.ModulateSelfOverride = palette.FooterStripe;
                break;
            case TextureButton textureButton:
                textureButton.ModulateSelfOverride = palette.Fg;
                break;
        }

        foreach (var child in control.Children)
            ApplyRecursive(child, palette);
    }

    private static void ThemePanel(PanelContainer panel, in PdaInteriorPalette palette)
    {
        if (panel.HasStyleClass("BackgroundDark"))
        {
            panel.PanelOverride = new StyleBoxFlat(palette.PanelBg);
            return;
        }

        if (panel.PanelOverride is StyleBoxFlat flat && flat.BackgroundColor.A > 0.2f)
        {
            var hsv = Color.ToHsv(flat.BackgroundColor);
            if (hsv.Y > 0.55f && flat.BackgroundColor.A > 0.9f && !float.IsNaN(panel.SetWidth) && panel.SetWidth <= 12)
                return;

            if (!IsDark(flat.BackgroundColor) && !IsLight(flat.BackgroundColor))
                return;

            panel.PanelOverride = new StyleBoxFlat(flat)
            {
                BackgroundColor = palette.ItemBg,
                BorderColor = flat.BorderColor.A > 0 ? palette.ButtonBorder : Color.Transparent,
            };
        }
    }

    private static void ThemeLabel(Label label, in PdaInteriorPalette palette)
    {
        if (label.FontColorOverride is not { } current || IsDark(current) || current == Color.Black)
            label.FontColorOverride = palette.Fg;
        else if (IsDarkGray(current))
            label.FontColorOverride = palette.FgMuted;
    }

    private static void ThemeButton(Button button, in PdaInteriorPalette palette)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = palette.ItemBg,
            BorderColor = palette.ButtonBorder,
            BorderThickness = new Thickness(1),
        };
        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
        button.StyleBoxOverride = box;

        foreach (var child in button.Children)
        {
            if (child is Label label)
                label.FontColorOverride = palette.Fg;
        }
    }

    private static void ThemeLineEdit(LineEdit lineEdit, in PdaInteriorPalette palette)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = palette.ItemHover,
            BorderColor = palette.ButtonBorder,
            BorderThickness = new Thickness(1),
        };
        box.SetContentMarginOverride(StyleBox.Margin.All, 4);
        lineEdit.StyleBoxOverride = box;
    }

    private static bool IsLight(Color color)
    {
        var hsv = Color.ToHsv(color);
        return hsv.Z >= 0.75f;
    }

    private static bool IsDark(Color color)
    {
        var hsv = Color.ToHsv(color);
        return hsv.Z <= 0.35f;
    }

    private static bool IsDarkGray(Color color)
    {
        var hsv = Color.ToHsv(color);
        return hsv.Z is >= 0.25f and <= 0.55f && hsv.Y < 0.25f;
    }
}
