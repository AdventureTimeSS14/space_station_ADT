using System.Globalization;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Drawing.UI;

public sealed class SliderEntry
{
    public readonly LineEdit Edit;

    private readonly Slider _slider;
    private readonly string _format;

    public SliderEntry(Slider slider, string format)
    {
        _slider = slider;
        _format = format;

        Edit = new LineEdit
        {
            MinWidth = 56,
        };
        Edit.OnTextEntered += _ => Commit();
        Edit.OnFocusExit += _ => Commit();

        Sync(true);
    }

    public void Sync(bool force = false)
    {
        if (!force && Edit.HasKeyboardFocus())
            return;

        Edit.Text = _slider.Value.ToString(_format, CultureInfo.InvariantCulture);
    }

    private void Commit()
    {
        var text = Edit.Text.Trim().Replace(',', '.');
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value))
            _slider.Value = Math.Clamp(value, _slider.MinValue, _slider.MaxValue);

        Sync(true);
    }
}
