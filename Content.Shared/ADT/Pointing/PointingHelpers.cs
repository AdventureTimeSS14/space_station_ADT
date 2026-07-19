using Robust.Shared.Utility;

namespace Content.Shared.ADT.Pointing;

public static class PointingHelpers
{
    public const string PointingChatColorHex = "#b8b0c7";

    /// <summary>
    /// Формирует сообщение для чата с иконкой сущности (через rich-text тег enttex).
    /// </summary>
    /// <param name="message">Текст сообщения (будет экранирован).</param>
    /// <param name="netEntityId">NetEntity id для отображения иконки. Если null - иконка не добавляется.</param>
    /// <param name="size">Размер иконки в пикселях.</param>
    /// <param name="scale">Масштаб иконки.</param>
    /// <param name="offsetX">Смещение по X.</param>
    /// <param name="offsetY">Смещение по Y.</param>
    /// <returns>Строка с rich-text разметкой.</returns>
    /// TODO: Экипированные ВЕЩИ тоже можно указать, но их спрайт будет криво смещён чуть выше. К сожалению не знаю как исправить. Но проблем не возникает.
    public static string BuildPointingChatMessage(string message, int? netEntityId, long size = 16, long scale = 1, long offsetX = 1, long offsetY = 0)
    {
        var escapedMessage = FormattedMessage.EscapeText(message);

        if (netEntityId == null)
            return escapedMessage;

        return $"{escapedMessage} [enttex id=\"{netEntityId}\" size=\"{size}\" scale=\"{scale}\" offsetX=\"{offsetX}\" offsetY=\"{offsetY}\"]";
    }
}