// SPDX-FileCopyrightText: 2025 LocalDuty
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.Lobby.UI;

/// <summary>
///     Рисует градиентное затемнение в левой части лобби.
///     Слева — непрозрачный чёрный, плавно переходящий в прозрачный справа.
/// </summary>
public sealed class LobbyVignette_Duty : Control
{
    // Ширина зоны затемнения в пикселях (от левого края)
    private const float VignetteWidth = 520f;

    // Максимальная непрозрачность затемнения (0..1)
    private const float MaxAlpha = 0.82f;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var gradientEnd = Math.Min(VignetteWidth, size.X);

        // Рисуем градиент полосками — от левого края до gradientEnd
        // Каждая полоска шириной 1px, alpha убывает линейно
        const int StepWidth = 2;
        for (var x = 0; x < (int)gradientEnd; x += StepWidth)
        {
            var t = 1f - (x / gradientEnd);  // 1.0 у левого края, 0.0 у правого
            // Плавность — кубическая кривая для более красивого перехода
            t = t * t * (3f - 2f * t);

            var alpha = t * MaxAlpha;
            var color = new Color(0f, 0f, 0f, alpha);

            handle.DrawRect(
                new UIBox2(x, 0, Math.Min(x + StepWidth, (int)gradientEnd), size.Y),
                color
            );
        }
    }
}
