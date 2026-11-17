using System.Numerics;
using Content.Shared.ADT.Chaplain.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Chat.Controls;

public sealed class PrayerHintLineEdit : HistoryLineEdit
{
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IPlayerManager _player = default!;

    private static readonly Color HintColor = Color.Gray.WithAlpha(0.7f);

    private string? _hint;

    public PrayerHintLineEdit()
    {
        IoCManager.InjectDependencies(this);
        OnTextChanged += _ => UpdateHint();
        OnTextEntered += OnPrayerEntered;
    }

    private void OnPrayerEntered(LineEditEventArgs args)
    {
        if (_player.LocalEntity is not { } local || !_systems.TryGetEntitySystem<SharedChaplainSystem>(out var chaplain))
            return;

        if (chaplain.TryMatchPrayer(local, args.Text, out var prayer))
            chaplain.RequestPrayer(prayer);
    }

    public bool TryCompleteHint()
    {
        if (string.IsNullOrEmpty(_hint) || CursorPosition != Text.Length)
            return false;

        SetText(Text + _hint, true);
        CursorPosition = Text.Length;
        return true;
    }

    private void UpdateHint()
    {
        _hint = null;

        if (_player.LocalEntity is not { } local || !_systems.TryGetEntitySystem<SharedChaplainSystem>(out var chaplain))
            return;

        _hint = chaplain.GetPrayerCompletion(local, Text);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (string.IsNullOrEmpty(_hint) || CursorPosition != Text.Length)
            return;

        var font = TryGetStyleProperty<Font>("font", out var styleFont)
            ? styleFont
            : UserInterfaceManager.ThemeDefaults.DefaultFont;

        var styleBox = TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box)
            ? box
            : UserInterfaceManager.ThemeDefaults.LineEditBox;

        var contentBox = styleBox.GetContentBox(PixelSizeBox, UIScale);
        var offsetY = (contentBox.Height - font.GetHeight(UIScale)) / 2f;
        var baseLine = new Vector2(GetOffsetAtIndex(Text.Length) * UIScale, contentBox.Top + offsetY + font.GetAscent(UIScale));

        foreach (var rune in _hint.EnumerateRunes())
        {
            if (baseLine.X > contentBox.Right)
                break;

            baseLine.X += font.DrawChar(handle, rune, baseLine, UIScale, HintColor);
        }
    }
}
