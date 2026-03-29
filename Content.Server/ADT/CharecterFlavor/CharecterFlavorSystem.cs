// Inspired by Nyanotrasen
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.CharecterFlavor;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    /// <summary>
    /// Вычисление SHA256 хэша для ключа кэша (только сервер, клиент не имеет доступа к криптографии)
    /// </summary>
    private static string ComputeSha256(string input)
    {
        var keyData = Encoding.UTF8.GetBytes(input);
        var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(keyData);
        return Convert.ToHexString(bytes);
    }

    [Dependency] private readonly IHttpClientHolder _httpClient = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    /// Кэш изображений: SHA256 хэш URL → (данные, время истечения)
    /// </summary>
    private readonly ConcurrentDictionary<string, (byte[] Data, TimeSpan Expires)> _imageCache = new();
    private readonly List<string> _cacheKeysSeq = new();

    /// <summary>
    /// Максимальное количество записей в кэше (по аналогии с TTSManager)
    /// </summary>
    private int _maxCachedCount = 100;

    public override void Initialize()
    {
        base.Initialize();

        _config.OnValueChanged(ADTCCVars.HeadshotCacheDuration, _ => PurgeExpiredCache(), true);
        _config.OnValueChanged(ADTCCVars.HeadshotMaxCacheCount, OnMaxCacheCountChanged, true);

        SubscribeNetworkEvent<RequestHeadshotPreviewEvent>(OnRequestHeadshotPreview);
    }

    private void OnMaxCacheCountChanged(int newValue)
    {
        _maxCachedCount = newValue;
        Logger.Debug($"Headshot max cache count changed to {newValue}");

        // Если кэш больше лимита, удаляем лишние записи
        while (_cacheKeysSeq.Count > _maxCachedCount && _cacheKeysSeq.Count > 0)
        {
            var firstKey = _cacheKeysSeq[0];
            _imageCache.TryRemove(firstKey, out _);
            _cacheKeysSeq.RemoveAt(0);
        }
    }

    public override void Shutdown()
    {
        _imageCache.Clear();
        _cacheKeysSeq.Clear();
        base.Shutdown();
    }

    protected override async void OpenFlavor(EntityUid actor, EntityUid target)
    {
        base.OpenFlavor(actor, target);

        if (!TryComp<CharacterFlavorComponent>(target, out var flavor))
            return;

        if (flavor.HeadshotUrl == string.Empty)
            return;

        var allowedDomain = _config.GetCVar(ADTCCVars.HeadshotDomain);
        if (!IsValidHeadshotUrl(flavor.HeadshotUrl, allowedDomain))
            return;

        try
        {
            var image = await GetImageAsync(flavor.HeadshotUrl);
            if (image == null)
                return;

            var ev = new SetHeadshotUiMessage(GetNetEntity(target), image);
            RaiseNetworkEvent(ev, actor);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to open flavor for {target}: {ex}");
        }
    }

    private async void OnRequestHeadshotPreview(RequestHeadshotPreviewEvent ev, EntitySessionEventArgs args)
    {
        var allowedDomain = _config.GetCVar(ADTCCVars.HeadshotDomain);
        if (!IsValidHeadshotUrl(ev.Url, allowedDomain))
        {
            Logger.Debug($"Invalid headshot URL from {args.SenderSession.Name}: {ev.Url}");
            return;
        }

        try
        {
            var image = await GetImageAsync(ev.Url);
            if (image == null)
            {
                Logger.Debug($"Failed to download headshot for {args.SenderSession.Name}: {ev.Url}");
                return;
            }

            RaiseNetworkEvent(new HeadshotPreviewEvent(image), Filter.SinglePlayer(args.SenderSession));
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to process headshot preview for {args.SenderSession.Name}: {ex}");
        }
    }

    /// <summary>
    /// Получить изображение из кэша или загрузить
    /// </summary>
    private async Task<byte[]?> GetImageAsync(string url)
    {
        // Используем SHA256 хэш URL как ключ кэша (как в TTSManager)
        var cacheKey = ComputeSha256(url);

        // Проверка кэша
        var now = _gameTiming.RealTime;
        if (_imageCache.TryGetValue(cacheKey, out var cached) && cached.Expires > now)
        {
            Logger.Debug($"Headshot cache hit: {cacheKey}");
            return cached.Data;
        }

        // Загрузка
        Logger.Debug($"Headshot cache miss, downloading: {url}");
        var image = await DownloadImageAsync(url);

        if (image != null)
        {
            // Сохранение в кэш с LRU eviction
            var cacheDuration = TimeSpan.FromMinutes(_config.GetCVar(ADTCCVars.HeadshotCacheDuration));

            // Удаляем старые записи если кэш переполнен
            while (_cacheKeysSeq.Count >= _maxCachedCount && _cacheKeysSeq.Count > 0)
            {
                var firstKey = _cacheKeysSeq[0];
                _imageCache.TryRemove(firstKey, out _);
                _cacheKeysSeq.RemoveAt(0);
            }

            _imageCache[cacheKey] = (image, now + cacheDuration);
            _cacheKeysSeq.Add(cacheKey);
        }

        return image;
    }

    /// <summary>
    /// Очистка просроченных записей кэша
    /// </summary>
    private void PurgeExpiredCache()
    {
        var now = _gameTiming.RealTime;
        var keysToRemove = new List<string>();

        foreach (var kvp in _imageCache)
        {
            if (kvp.Value.Expires < now)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _imageCache.TryRemove(key, out _);
            _cacheKeysSeq.Remove(key);
        }

        Logger.Debug($"Purged {keysToRemove.Count} expired headshot cache entries");
    }

    /// <summary>
    /// Загрузка изображения с сервера (использует ReadAsByteArrayAsync по аналогии с TTSManager)
    /// </summary>
    private async Task<byte[]?> DownloadImageAsync(string url)
    {
        try
        {
            var maxSize = _config.GetCVar(ADTCCVars.HeadshotMaxSize);
            var timeout = (float)_config.GetCVar(ADTCCVars.HeadshotCacheDuration) * 60 * 1000; // ms
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));

            using var response = await _httpClient.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Debug($"Headshot download failed with status: {response.StatusCode}");
                return null;
            }

            // Проверка Content-Length
            if (response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength.Value > maxSize)
            {
                Logger.Debug($"Headshot too large: {response.Content.Headers.ContentLength.Value} bytes");
                return null;
            }

            // Используем ReadAsByteArrayAsync вместо ручного чтения (по аналогии с TTSManager)
            var image = await response.Content.ReadAsByteArrayAsync(cts.Token);

            if (image.Length > maxSize)
            {
                Logger.Debug($"Headshot exceeds max size after download: {image.Length} bytes");
                return null;
            }

            Logger.Debug($"Headshot downloaded: {image.Length} bytes");

            return image;
        }
        catch (TaskCanceledException)
        {
            Logger.Debug($"Headshot download timeout: {url}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download headshot from {url}: {ex}");
            return null;
        }
    }
}
