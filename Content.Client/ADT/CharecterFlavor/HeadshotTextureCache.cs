using System.Collections.Concurrent;
using Robust.Client.Graphics;

namespace Content.Client.ADT.CharecterFlavor;

/// <summary>
/// Глобальный кэш текстур для headshot изображений.
/// </summary>
public static class HeadshotTextureCache
{
    private static readonly ConcurrentDictionary<int, (Texture Texture, byte[] Bytes)> _textureCache = new();

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
            // Вычисляем быстрый int хэш
            var hash = ComputeHash(imageBytes);

            // Проверка кэша
            if (_textureCache.TryGetValue(hash, out var cached))
            {
                if (ByteArraysEqual(cached.Bytes, imageBytes))
                {
                    Logger.DebugS("headshot", $"Headshot texture cache hit: {hash}");
                    return cached.Texture;
                }
            }

            // Загрузка текстуры
            using var stream = new System.IO.MemoryStream(imageBytes, writable: false);
            using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream);
            var texture = clyde.LoadTextureFromImage(image);

            // Сохранение в кэш
            _textureCache[hash] = (texture, imageBytes);
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
    /// Быстрый хэш для кэширования
    /// </summary>
    private static int ComputeHash(byte[] data)
    {
        unchecked
        {
            int hash = 17;
            foreach (var b in data)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }

    /// <summary>
    /// Сравнение двух байтовых массивов
    /// </summary>
    private static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Очистить кэш текстур
    /// </summary>
    public static void Clear()
    {
        _textureCache.Clear();
    }
}
