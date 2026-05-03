// SPDX-FileCopyrightText: 2025 LocalDuty
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Client.Parallax.Data;
using Content.Client.Parallax.Managers;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.MainMenu.UI;

/// <summary>
///     Параллакс-фон главного меню LocalDuty.
///     Слой 0 (space-bg)  — почти неподвижен, реагирует на курсор слабо.
///     Слой 1 (stars-bg)  — реагирует на курсор заметнее.
/// </summary>
public sealed class MainMenuParallaxControl_Duty : Control
{
    [Dependency] private readonly IGameTiming _timing      = default!;
    [Dependency] private readonly IParallaxManager _parallax = default!;
    [Dependency] private readonly IInputManager _input      = default!;

    private const string PrototypeName = "ParallaxDuty";

    // Насколько курсор смещает каждый слой.
    // Значение = максимальное смещение в пикселях при курсоре у края экрана.
    private const float Layer0MouseStrength = 12f;  // фон — почти стоит
    private const float Layer1MouseStrength = 36f;  // звёзды — заметно двигаются

    // Плавность — насколько быстро текущее смещение «догоняет» целевое (0..1 за кадр).
    private const float Smoothing = 0.07f;

    // Текущее сглаженное смещение для каждого слоя.
    private Vector2 _currentOffset0 = Vector2.Zero;
    private Vector2 _currentOffset1 = Vector2.Zero;

    public MainMenuParallaxControl_Duty()
    {
        IoCManager.InjectDependencies(this);

        RectClipContent = true;

        _parallax.LoadParallaxByName(PrototypeName);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // ── Вычисляем целевое смещение на основе позиции курсора ──────────────
        var screenSize  = Size;                           // размер контрола в UI-пикселях
        var mousePos    = _input.MouseScreenPosition.Position; // позиция мыши на экране

        // Нормализованное отклонение курсора от центра: диапазон [-1 .. +1]
        Vector2 normalizedDelta;
        if (screenSize.X > 0 && screenSize.Y > 0)
        {
            normalizedDelta = new Vector2(
                (float)(mousePos.X / screenSize.X) * 2f - 1f,
                (float)(mousePos.Y / screenSize.Y) * 2f - 1f
            );
            // Clamp — на случай если мышь за пределами окна
            normalizedDelta = Vector2.Clamp(normalizedDelta, new Vector2(-1f), new Vector2(1f));
        }
        else
        {
            normalizedDelta = Vector2.Zero;
        }

        var targetOffset0 = normalizedDelta * Layer0MouseStrength;
        var targetOffset1 = normalizedDelta * Layer1MouseStrength;

        // ── Плавное сглаживание (lerp) ─────────────────────────────────────────
        _currentOffset0 = Vector2.Lerp(_currentOffset0, targetOffset0, Smoothing);
        _currentOffset1 = Vector2.Lerp(_currentOffset1, targetOffset1, Smoothing);

        // ── Рисуем слои ────────────────────────────────────────────────────────
        var layers = _parallax.GetParallaxLayers(PrototypeName);

        for (var i = 0; i < layers.Length; i++)
        {
            var layer      = layers[i];
            var tex        = layer.Texture;
            var ourSize    = PixelSize;

            // Масштабируем текстуру под ширину экрана (как в оригинальном ParallaxControl)
            var texSize = new Vector2i(
                (int)(tex.Size.X * Size.X * layer.Config.Scale.X / 1920f),
                (int)(tex.Size.Y * Size.X * layer.Config.Scale.Y / 1920f)
            );

            texSize.X = Math.Max(texSize.X, 1);
            texSize.Y = Math.Max(texSize.Y, 1);

            // Смещение на основе курсора для текущего слоя
            var mouseOffset = i == 0 ? _currentOffset0 : _currentOffset1;

            if (layer.Config.Tiled)
            {
                // Для тайлового слоя применяем только смещение мыши
                var scaledOffset = mouseOffset.Floored();

                // Модуло чтобы не рисовать лишние тайлы за экраном
                scaledOffset.X = ((scaledOffset.X % texSize.X) + texSize.X) % texSize.X;
                scaledOffset.Y = ((scaledOffset.Y % texSize.Y) + texSize.Y) % texSize.Y;

                for (var x = -scaledOffset.X; x < ourSize.X; x += texSize.X)
                {
                    for (var y = -scaledOffset.Y; y < ourSize.Y; y += texSize.Y)
                    {
                        handle.DrawTextureRect(tex, UIBox2.FromDimensions(new Vector2(x, y), texSize));
                    }
                }
            }
            else
            {
                // Не тайловый — рисуем по центру со смещением мыши
                var origin = ((ourSize - texSize) / 2)
                             + layer.Config.ControlHomePosition
                             + mouseOffset;
                handle.DrawTextureRect(tex, UIBox2.FromDimensions(origin, texSize));
            }
        }
    }
}