// Inspired by Nyanotrasen
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.CharecterFlavor;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Server.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan FailedCacheDuration = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan PreviewRequestInterval = TimeSpan.FromSeconds(1);

    private const int MaxSourceDimension = 4096;

    private const int MaxOutputDimension = 1024;

    private static readonly DecoderOptions ImageDecoderOptions = new()
    {
        Configuration = new SixLabors.ImageSharp.Configuration(
            new PngConfigurationModule(),
            new JpegConfigurationModule(),
            new WebpConfigurationModule(),
            new GifConfigurationModule()),
        MaxFrames = 1,
    };

    private HttpClient _httpClient = default!;

    private readonly Dictionary<string, (byte[]? Data, TimeSpan Expires)> _imageCache = new();
    private readonly List<string> _cacheKeysSeq = new();
    private readonly object _cacheLock = new();

    private readonly Dictionary<string, Task<byte[]?>> _inFlight = new();
    private readonly SemaphoreSlim _processingLimit = new(2);

    private readonly Dictionary<NetUserId, TimeSpan> _lastPreviewRequest = new();

    private int _maxCachedCount = 100;

    public override void Initialize()
    {
        base.Initialize();

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(5),
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = DownloadTimeout,
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SS14-ADT-Headshot/1.0");

        _config.OnValueChanged(ADTCCVars.HeadshotCacheDuration, _ => PurgeExpiredCache(), true);
        _config.OnValueChanged(ADTCCVars.HeadshotMaxCacheCount, OnMaxCacheCountChanged, true);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<RequestHeadshotPreviewEvent>(OnRequestHeadshotPreview);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _httpClient.Dispose();

        lock (_cacheLock)
        {
            _imageCache.Clear();
            _cacheKeysSeq.Clear();
            _inFlight.Clear();
        }

        _lastPreviewRequest.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _lastPreviewRequest.Remove(e.Session.UserId);
    }

    private void OnMaxCacheCountChanged(int newValue)
    {
        _maxCachedCount = newValue;

        lock (_cacheLock)
        {
            TrimCache(_maxCachedCount);
        }
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

            if (TerminatingOrDeleted(target))
                return;

            var ev = new SetHeadshotUiMessage(GetNetEntity(target), image);
            RaiseNetworkEvent(ev, actor);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open flavor for {ToPrettyString(target)}: {ex}");
        }
    }

    private async void OnRequestHeadshotPreview(RequestHeadshotPreviewEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        var now = _gameTiming.RealTime;
        if (_lastPreviewRequest.TryGetValue(session.UserId, out var last) && now - last < PreviewRequestInterval)
            return;

        _lastPreviewRequest[session.UserId] = now;

        var allowedDomain = _config.GetCVar(ADTCCVars.HeadshotDomain);
        if (!IsValidHeadshotUrl(ev.Url, allowedDomain))
        {
            Log.Debug($"Invalid headshot URL from {session.Name}: {ev.Url}");
            return;
        }

        try
        {
            var image = await GetImageAsync(ev.Url);
            if (image == null)
            {
                Log.Debug($"Failed to download headshot for {session.Name}: {ev.Url}");
                return;
            }

            if (session.Status == SessionStatus.Disconnected)
                return;

            RaiseNetworkEvent(new HeadshotPreviewEvent(image), Filter.SinglePlayer(session));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to process headshot preview for {session.Name}: {ex}");
        }
    }

    private async Task<byte[]?> GetImageAsync(string url)
    {
        var now = _gameTiming.RealTime;
        Task<byte[]?>? task;

        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(url, out var cached) && cached.Expires > now)
                return cached.Data;

            if (!_inFlight.TryGetValue(url, out task))
            {
                task = Task.Run(() => LoadImageAsync(url));
                _inFlight[url] = task;
            }
        }

        return await task;
    }

    private async Task<byte[]?> LoadImageAsync(string url)
    {
        try
        {
            var raw = await DownloadImageAsync(url);

            byte[]? image = null;
            if (raw != null)
            {
                await _processingLimit.WaitAsync();
                try
                {
                    image = SanitizeImage(raw, url);
                }
                finally
                {
                    _processingLimit.Release();
                }
            }

            var duration = image != null
                ? TimeSpan.FromMinutes(_config.GetCVar(ADTCCVars.HeadshotCacheDuration))
                : FailedCacheDuration;

            lock (_cacheLock)
            {
                if (_maxCachedCount > 0)
                {
                    if (!_imageCache.ContainsKey(url))
                    {
                        TrimCache(_maxCachedCount - 1);
                        _cacheKeysSeq.Add(url);
                    }

                    _imageCache[url] = (image, _gameTiming.RealTime + duration);
                }
            }

            return image;
        }
        finally
        {
            lock (_cacheLock)
            {
                _inFlight.Remove(url);
            }
        }
    }

    private void TrimCache(int limit)
    {
        while (_cacheKeysSeq.Count > Math.Max(limit, 0))
        {
            _imageCache.Remove(_cacheKeysSeq[0]);
            _cacheKeysSeq.RemoveAt(0);
        }
    }

    private void PurgeExpiredCache()
    {
        var now = _gameTiming.RealTime;

        lock (_cacheLock)
        {
            for (var i = _cacheKeysSeq.Count - 1; i >= 0; i--)
            {
                var key = _cacheKeysSeq[i];
                if (_imageCache.TryGetValue(key, out var entry) && entry.Expires >= now)
                    continue;

                _imageCache.Remove(key);
                _cacheKeysSeq.RemoveAt(i);
            }
        }
    }

    private async Task<byte[]?> DownloadImageAsync(string url)
    {
        var maxSize = _config.GetCVar(ADTCCVars.HeadshotMaxSize);

        try
        {
            using var cts = new CancellationTokenSource(DownloadTimeout);
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                Log.Debug($"Headshot download failed with status {response.StatusCode}: {url}");
                return null;
            }

            if (response.Content.Headers.ContentLength > maxSize)
            {
                Log.Debug($"Headshot too large ({response.Content.Headers.ContentLength} bytes): {url}");
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var memory = new MemoryStream();
            var buffer = new byte[81920];

            int read;
            while ((read = await stream.ReadAsync(buffer, cts.Token)) > 0)
            {
                if (memory.Length + read > maxSize)
                {
                    Log.Debug($"Headshot exceeds max size while downloading: {url}");
                    return null;
                }

                memory.Write(buffer, 0, read);
            }

            return memory.ToArray();
        }
        catch (OperationCanceledException)
        {
            Log.Debug($"Headshot download timeout: {url}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Log.Debug($"Headshot download failed: {url}: {ex.Message}");
            return null;
        }
    }

    private byte[]? SanitizeImage(byte[] raw, string url)
    {
        try
        {
            var info = Image.Identify(ImageDecoderOptions, raw);
            if (info.Width <= 0 || info.Height <= 0 ||
                info.Width > MaxSourceDimension || info.Height > MaxSourceDimension)
            {
                Log.Debug($"Headshot has bad dimensions {info.Width}x{info.Height}: {url}");
                return null;
            }

            using var image = Image.Load<Rgba32>(ImageDecoderOptions, raw);

            if (image.Width > MaxOutputDimension || image.Height > MaxOutputDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(MaxOutputDimension, MaxOutputDimension),
                    Mode = ResizeMode.Max,
                }));
            }

            using var output = new MemoryStream();
            image.SaveAsPng(output);
            return output.ToArray();
        }
        catch (Exception ex) when (ex is ImageFormatException or NotSupportedException)
        {
            Log.Debug($"Headshot is not a valid image: {url}: {ex.Message}");
            return null;
        }
    }
}
