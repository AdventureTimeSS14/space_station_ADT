using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.ADT.Stylesheets;

/// <summary>
/// Оранжевая раскраска кнопок
/// </summary>
[CommonSheetlet]
public sealed class OrangeButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    public const string StyleClassButtonOrange = "ButtonColorOrange";

    private static readonly ColorPalette OrangePalette =
        ColorPalette.FromHexBase("#e77719", element: Color.FromHex("#e77719"));

    public override StyleRule[] GetRules(T sheet, object config)
    {
        return
        [
            OrangeButton().PseudoNormal().Prop(Control.StylePropertyModulateSelf, OrangePalette.Element),
            OrangeButton().PseudoHovered().Prop(Control.StylePropertyModulateSelf, OrangePalette.HoveredElement),
            OrangeButton().PseudoPressed().Prop(Control.StylePropertyModulateSelf, OrangePalette.PressedElement),
            OrangeButton().PseudoDisabled().Prop(Control.StylePropertyModulateSelf, OrangePalette.DisabledElement),
        ];
    }

    private static MutableSelectorElement OrangeButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(StyleClassButtonOrange);
    }
}
