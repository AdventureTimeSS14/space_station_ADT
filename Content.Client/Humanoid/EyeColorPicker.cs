using Content.Client.ADT.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Humanoid;

public sealed class EyeColorPicker : Control
{
    public event Action<Color>? OnEyeColorPicked;

    private readonly LegacyColorSelectorSliders _colorSelectors;    // ADT-Tweak - ColorSelectorSliders > LegacyColorSelectorSliders

    private Color _lastColor;

    public void SetData(Color color)
    {
        _lastColor = color;

        _colorSelectors.Color = color;
    }

    public EyeColorPicker()
    {
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        AddChild(vBox);

        vBox.AddChild(_colorSelectors = new LegacyColorSelectorSliders());  // ADT-Tweak - ColorSelectorSliders > LegacyColorSelectorSliders
        // ADT-Tweak-Start
        // _colorSelectors.SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv; // defaults color selector to HSV
        // ADT-Tweak-End

        _colorSelectors.OnColorChanged += ColorValueChanged;
    }

    private void ColorValueChanged(Color newColor)
    {
        OnEyeColorPicked?.Invoke(newColor);

        _lastColor = newColor;
    }
}
