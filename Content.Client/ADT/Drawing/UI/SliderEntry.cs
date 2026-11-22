using System.Globalization;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Drawing.UI;

public sealed class SliderEntry
{
    public readonly LineEdit Edit;

    private readonly Slider _slider;
    private readonly string _format;
    private string _synced = string.Empty;

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

        _synced = _slider.Value.ToString(_format, CultureInfo.InvariantCulture);
        Edit.Text = _synced;
    }

    private void Commit()
    {
        if (Edit.Text == _synced)
            return;

        var text = Edit.Text.Trim().Replace(',', '.');
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value))
            _slider.Value = Math.Clamp(value, _slider.MinValue, _slider.MaxValue);

        Sync(true);
    }
}
