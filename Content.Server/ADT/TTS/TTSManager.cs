using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared.ADT.CCVar;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server.ADT.TTS;

public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration
        {
            LabelNames = ["type"],
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache or from an already running request.");

    private static readonly Counter DroppedCount = Metrics.CreateCounter(
        "tts_dropped_count",
        "Amount of TTS requests dropped without hitting the API.");

    private static readonly Counter CircuitOpenCount = Metrics.CreateCounter(
        "tts_circuit_open_count",
        "Amount of times the TTS circuit breaker has been opened.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private HttpClient _httpClient = default!;
    private ISawmill _sawmill = default!;

    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly Queue<string> _cacheOrder = new();
    private readonly Dictionary<string, Task<byte[]?>> _inFlight = new();
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();

    private int _activeRequests;
    private int _maxCachedCount;
    private int _maxConcurrent;
    private int _maxQueued;
    private int _breakerFailures;
    private float _breakerCooldown;

    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;
    private bool _isEnabled;

    private int _consecutiveFailures;
    private DateTime _circuitOpenedAt;
    private CircuitState _circuit = CircuitState.Closed;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");

        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 128,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(5),
        });

        _cfg.OnValueChanged(ADTTTSCVars.TTSEnabled, v =>
        {
            _isEnabled = v;

            if (!v)
                ResetCache();
        }, true);

        _cfg.OnValueChanged(ADTTTSCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);

        _cfg.OnValueChanged(ADTTTSCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSApiToken, v => _apiToken = v, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSMaxConcurrentRequests, v => _maxConcurrent = Math.Max(1, v), true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSMaxQueuedRequests, v => _maxQueued = Math.Max(0, v), true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSCircuitBreakerFailures, v => _breakerFailures = v, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSCircuitBreakerCooldown, v => _breakerCooldown = v, true);
    }

    /// <summary>
    /// Generates audio for the provided text.
    /// </summary>
    /// <param name="speaker">Voice identifier.</param>
    /// <param name="text">Dialogue text.</param>
    /// <param name="effect">Service effect.</param>
    /// <returns>OGG bytes, or null if generation failed.</returns>
    public Task<byte[]?> ConvertTextToSpeech(string speaker, string text, string? effect = null)
    {
        WantedCount.Inc();

        if (!_isEnabled)
        {
            DroppedCount.Inc();
            return Task.FromResult<byte[]?>(null);
        }

        var cacheKey = GenerateCacheKey(speaker, text, effect);

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            ReusedCount.Inc();
            _sawmill.Verbose($"Use cached sound for '{text}' speech by '{speaker}' speaker");
            return Task.FromResult<byte[]?>(cached);
        }

        if (_inFlight.TryGetValue(cacheKey, out var running))
        {
            ReusedCount.Inc();
            _sawmill.Verbose($"Join running request for '{text}' speech by '{speaker}' speaker");
            return running;
        }

        var task = RequestAsync(speaker, text, effect, cacheKey);

        if (!task.IsCompleted)
            _inFlight[cacheKey] = task;

        return task;
    }

    private async Task<byte[]?> RequestAsync(string speaker, string text, string? effect, string cacheKey)
    {
        try
        {
            if (IsCircuitBlocking())
            {
                DroppedCount.Inc();
                return null;
            }

            var timeout = TimeSpan.FromSeconds(_cfg.GetCVar(ADTTTSCVars.TTSApiTimeout));
            using var cts = new CancellationTokenSource(timeout);

            if (!await TryEnterAsync(cts.Token))
            {
                DroppedCount.Inc();
                _sawmill.Warning($"TTS request queue is full, dropped speech by '{speaker}' speaker");
                return null;
            }

            try
            {
                if (!TryPassCircuitBreaker())
                {
                    DroppedCount.Inc();
                    return null;
                }

                return await SendAsync(speaker, text, effect, cacheKey, cts.Token);
            }
            finally
            {
                Release();
            }
        }
        finally
        {
            _inFlight.Remove(cacheKey);
        }
    }

    private async Task<byte[]?> SendAsync(string speaker, string text, string? effect, string cacheKey, CancellationToken ct)
    {
        _sawmill.Verbose($"Generate new audio for '{text}' speech by '{speaker}' speaker");

        var reqTime = DateTime.UtcNow;
        try
        {
            var url = $"{_apiUrl}?speaker={speaker}&text={HttpUtility.UrlEncode(text)}&ext=ogg";
            if (!string.IsNullOrEmpty(effect))
                url += $"&effect={HttpUtility.UrlEncode(effect)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS request was rate limited");
                    ReportSuccess();
                    return null;
                }

                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                ReportFailure();
                return null;
            }

            var soundData = await response.Content.ReadAsByteArrayAsync(ct);

            if (_cache.TryAdd(cacheKey, soundData))
            {
                _cacheOrder.Enqueue(cacheKey);
                while (_cache.Count > _maxCachedCount && _cacheOrder.TryDequeue(out var oldest))
                {
                    _cache.Remove(oldest);
                }
            }

            _sawmill.Debug($"Generated new audio for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            ReportSuccess();

            return soundData;
        }
        catch (OperationCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
            ReportFailure();
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            ReportFailure();
            return null;
        }
    }

    public void ResetCache()
    {
        _cache.Clear();
        _cacheOrder.Clear();
    }

    private async Task<bool> TryEnterAsync(CancellationToken ct)
    {
        if (_activeRequests < _maxConcurrent)
        {
            _activeRequests++;
            return true;
        }

        if (_waiters.Count >= _maxQueued)
            return false;

        var waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiters.Enqueue(waiter);

        await using var registration = ct.Register(() => waiter.TrySetResult(false));
        return await waiter.Task;
    }

    private void Release()
    {
        while (_waiters.TryDequeue(out var waiter))
        {
            if (waiter.TrySetResult(true))
                return;
        }

        _activeRequests--;
    }

    private enum CircuitState : byte
    {
        /// <summary>Service is considered healthy, requests are sent.</summary>
        Closed,
        /// <summary>Service is considered unavailable, requests are rejected without accessing the network.</summary>
        Open,
        /// <summary>Allows one test request through and rejects the rest.</summary>
        HalfOpen,
    }

    private bool IsCircuitBlocking()
    {
        if (_breakerFailures <= 0)
            return false;

        return _circuit switch
        {
            CircuitState.Open => DateTime.UtcNow - _circuitOpenedAt < TimeSpan.FromSeconds(_breakerCooldown),
            CircuitState.HalfOpen => true,
            _ => false,
        };
    }

    private bool TryPassCircuitBreaker()
    {
        if (_breakerFailures <= 0)
            return true;

        switch (_circuit)
        {
            case CircuitState.Closed:
                return true;

            case CircuitState.Open:
                if (DateTime.UtcNow - _circuitOpenedAt < TimeSpan.FromSeconds(_breakerCooldown))
                    return false;

                _circuit = CircuitState.HalfOpen;
                _sawmill.Info("TTS circuit breaker is probing the service");
                return true;

            case CircuitState.HalfOpen:
                return false;

            default:
                return true;
        }
    }

    private void ReportSuccess()
    {
        _consecutiveFailures = 0;

        if (_circuit == CircuitState.Closed)
            return;

        _circuit = CircuitState.Closed;
        _sawmill.Info("TTS circuit breaker is closed, service responds again");
    }

    private void ReportFailure()
    {
        if (_breakerFailures <= 0)
            return;

        _consecutiveFailures++;

        if (_circuit != CircuitState.HalfOpen && _consecutiveFailures < _breakerFailures)
            return;

        _circuit = CircuitState.Open;
        _circuitOpenedAt = DateTime.UtcNow;
        CircuitOpenCount.Inc();
        _sawmill.Warning(
            $"TTS service is unavailable, dropping requests for {_breakerCooldown} seconds");
    }

    private static string GenerateCacheKey(string speaker, string text, string? effect)
    {
        var keyData = Encoding.UTF8.GetBytes($"{speaker}/{effect}/{text}");
        return Convert.ToHexString(SHA256.HashData(keyData));
    }
}
