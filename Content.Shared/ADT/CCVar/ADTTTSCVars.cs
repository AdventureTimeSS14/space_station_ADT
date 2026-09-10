using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class ADTTTSCVars
{
    /// <summary>
    ///     Enables or disables TTS.
    /// </summary>
    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    ///     Enables or disables radio TTS on the server.
    /// </summary>
    public static readonly CVarDef<bool> TTSRadioEnabled =
        CVarDef.Create("tts.radio.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    ///     TTS service API URL.
    /// </summary>
    public static readonly CVarDef<string> TTSApiUrl =
        CVarDef.Create("tts.api_url", "", CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     API authorization token.
    /// </summary>
    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     API request timeout in seconds.
    /// </summary>
    public static readonly CVarDef<int> TTSApiTimeout =
        CVarDef.Create("tts.api_timeout", 5, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Client-side TTS volume.
    /// </summary>
    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Enables or disables radio TTS on the client.
    /// </summary>
    public static readonly CVarDef<bool> TTSRadioClientEnabled =
        CVarDef.Create("tts.radio.enabled_client", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Overall radio TTS volume.
    /// </summary>
    public static readonly CVarDef<float> TTSRadioVolume =
        CVarDef.Create("tts.radio.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Volume of individual radio channels.
    /// </summary>
    public static readonly CVarDef<string> TTSRadioChannelVolumes =
        CVarDef.Create("tts.radio.channel_volumes", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Maximum cache size for generated speech.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxCache =
        CVarDef.Create("tts.max_cache", 250, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Rate limit period in seconds.
    /// </summary>
    public static readonly CVarDef<float> TTSRateLimitPeriod =
        CVarDef.Create("tts.rate_limit_period", 2f, CVar.SERVERONLY);

    /// <summary>
    ///     Maximum requests allowed per rate limit period.
    /// </summary>
    public static readonly CVarDef<int> TTSRateLimitCount =
        CVarDef.Create("tts.rate_limit_count", 3, CVar.SERVERONLY);

    /// <summary>
    ///     Maximum number of concurrent requests to the service.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxConcurrentRequests =
        CVarDef.Create("tts.max_concurrent_requests", 32, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Maximum number of requests waiting in the queue.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxQueuedRequests =
        CVarDef.Create("tts.max_queued_requests", 256, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Number of failures required to trigger the circuit breaker.
    ///     Zero disables the protection.
    /// </summary>
    public static readonly CVarDef<int> TTSCircuitBreakerFailures =
        CVarDef.Create("tts.circuit_breaker_failures", 20, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Circuit breaker cooldown before a test request.
    /// </summary>
    public static readonly CVarDef<float> TTSCircuitBreakerCooldown =
        CVarDef.Create("tts.circuit_breaker_cooldown", 15f, CVar.SERVERONLY | CVar.ARCHIVE);
}
