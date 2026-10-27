using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.ADT.Stylesheets;

/// <summary>
/// Button gold!!
/// </summary>
[CommonSheetlet]
public sealed class GoldButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    public const string StyleClassButtonGold = "ButtonColorGold";

    private static readonly ColorPalette GoldPalette = ColorPalette.FromHexBase("#a68202");

    public override StyleRule[] GetRules(T sheet, object config)
    {
        return
        [
            GoldButton().PseudoNormal().Prop(Control.StylePropertyModulateSelf, GoldPalette.Element),
            GoldButton().PseudoHovered().Prop(Control.StylePropertyModulateSelf, GoldPalette.HoveredElement),
            GoldButton().PseudoPressed().Prop(Control.StylePropertyModulateSelf, GoldPalette.PressedElement),
            GoldButton().PseudoDisabled().Prop(Control.StylePropertyModulateSelf, GoldPalette.DisabledElement),
        ];
    }

    private static MutableSelectorElement GoldButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(StyleClassButtonGold);
    }
}
