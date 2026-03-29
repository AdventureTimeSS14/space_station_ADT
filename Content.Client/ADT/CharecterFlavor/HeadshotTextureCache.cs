using System.Collections.Concurrent;
using Robust.Client.Graphics;

namespace Content.Client.ADT.CharecterFlavor;

/// <summary>
/// Глобальный кэш текстур для headshot изображений.
/// </summary>
public static class HeadshotTextureCache
{
    private static readonly ConcurrentDictionary<int, Texture> _textureCache = new();

    /// <summary>
    /// Получить текстуру из кэша или загрузить новую
    /// </summary>
    /// <param name="imageBytes">Байты изображения</param>
    /// <param name="clyde">IClyde для загрузки текстуры</param>
    /// <returns>Texture или null при ошибке</returns>
    public static Texture? GetOrLoadTexture(byte[] imageBytes, IClyde clyde)
    {
        try
        {
            // Вычисляем хэш для кэширования
            var hash = Shared.ADT.CharecterFlavor.HeadshotHashHelper.ComputeHash(imageBytes);

            // Проверка кэша
            if (_textureCache.TryGetValue(hash, out var cachedTexture))
            {
                Logger.DebugS("headshot", $"Headshot texture cache hit: {hash}");
                return cachedTexture;
            }

            // Загрузка текстуры
            using var stream = new System.IO.MemoryStream(imageBytes, writable: false);
            using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream);
            var texture = clyde.LoadTextureFromImage(image);

            // Сохранение в кэш
            _textureCache[hash] = texture;
            Logger.DebugS("headshot", $"Headshot texture cache miss, loaded: {hash}");

            return texture;
        }
        catch (Exception ex)
        {
            Logger.DebugS("headshot", $"Failed to load headshot texture: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Очистить кэш текстур
    /// </summary>
    public static void Clear()
    {
        _textureCache.Clear();
    }
}
