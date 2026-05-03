// SPDX-FileCopyrightText: 2025 LocalDuty
//
// SPDX-License-Identifier: MIT

using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

/// <summary>
///     Кнопка лобби с плавной анимацией при наведении:
///     — цвет: #4E5754 → #FFFAFA
///     — сдвиг вправо: 0 → 12px
///     Скорость анимации регулируется полем AnimSpeed.
/// </summary>
public sealed class LobbyAnimatedButton : Button
{
    /// <summary>Скорость интерполяции (единиц в секунду, 0..1).</summary>
    public float AnimSpeed = 8f;

    private float _progress = 0f;

    private static readonly Color ColorNormal = Color.FromHex("#4E5754");
    private static readonly Color ColorHover  = Color.FromHex("#FFFAFA");
    private const float MaxShift = 12f;

    public LobbyAnimatedButton()
    {
        // Сбрасываем стандартный StyleBox SS14 через StyleBoxOverride
        // — рисуем только текст, без рамки и фона
        var emptyBox = new StyleBoxEmpty();
        emptyBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);
        StyleBoxOverride = emptyBox;

        AddStyleClass(StyleLobbyDuty_Const.LobbyButtonDuty);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        if (Label != null)
            Label.FontColorOverride = ColorNormal;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var target = IsHovered ? 1f : 0f;
        _progress = MathHelper.Clamp(
            _progress + (target - _progress) * MathF.Min(1f, AnimSpeed * args.DeltaSeconds),
            0f, 1f
        );

        var col = Color.InterpolateBetween(ColorNormal, ColorHover, _progress);
        if (Label != null)
            Label.FontColorOverride = col;

        var shift = _progress * MaxShift;
        Margin = new Thickness(shift, Margin.Top, 0, Margin.Bottom);
    }
}
