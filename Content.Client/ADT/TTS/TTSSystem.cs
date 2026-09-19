using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.TTS;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.TTS;

public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;
    private static readonly MemoryContentRoot ContentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private static bool _contentRootAdded;

    private const float WhisperFade = 4f;
    private const float MinimalVolume = -10f;

    private float _volume;
    private int _fileIdx;
    private bool _isEnabled;

    private bool _radioEnabled;
    private float _radioVolume;
    private Dictionary<string, float> _channelVolumes = new();

    private EntityUid? _previewStream;

    public override void Initialize()
    {
        if (!_contentRootAdded)
        {
            _contentRootAdded = true;
            _res.AddRoot(Prefix, ContentRoot);
        }

        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(ADTTTSCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSEnabled, OnTtsEnabledChanged, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSRadioClientEnabled, OnRadioEnabledChanged, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSRadioVolume, OnRadioVolumeChanged, true);
        _cfg.OnValueChanged(ADTTTSCVars.TTSRadioChannelVolumes, OnChannelVolumesChanged, true);

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(ADTTTSCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(ADTTTSCVars.TTSEnabled, OnTtsEnabledChanged);
        _cfg.UnsubValueChanged(ADTTTSCVars.TTSRadioClientEnabled, OnRadioEnabledChanged);
        _cfg.UnsubValueChanged(ADTTTSCVars.TTSRadioVolume, OnRadioVolumeChanged);
        _cfg.UnsubValueChanged(ADTTTSCVars.TTSRadioChannelVolumes, OnChannelVolumesChanged);
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsEnabledChanged(bool enabled)
    {
        _isEnabled = enabled;
    }

    private void OnRadioEnabledChanged(bool enabled)
    {
        _radioEnabled = enabled;
    }

    private void OnRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnChannelVolumesChanged(string raw)
    {
        _channelVolumes = TTSRadioVolumes.Parse(raw);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        if (!_isEnabled)
            return;

        if (_cfg.GetCVar(ADTCCVars.ReplaceTTSWithBarks))
            return;

        if (HasComp<DeafTraitComponent>(_playerManager.LocalEntity))
            return;

        var gain = ev.Kind == TTSKind.Radio ? GetRadioGain(ev.Channel) : _volume;
        if (gain <= 0f)
            return;

        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        ContentRoot.AddOrUpdateFile(filePath, ev.Data);

        try
        {
            var audioResource = new AudioResource();
            audioResource.Load(IoCManager.Instance!, Prefix / filePath);

            var audioParams = AudioParams.Default
                .WithVolume(AdjustVolume(gain, ev.IsWhisper))
                .WithMaxDistance(AdjustDistance(ev.IsWhisper));

            var soundSpecifier = new ResolvedPathSpecifier(Prefix / filePath);

            if (ev.SourceUid != null)
            {
                if (!TryGetEntity(ev.SourceUid.Value, out var sourceUid))
                    return;

                _audio.PlayEntity(audioResource.AudioStream, sourceUid.Value, soundSpecifier, audioParams);
                return;
            }

            if (ev.Kind == TTSKind.Preview)
                _audio.Stop(_previewStream);

            var stream = _audio.PlayGlobal(audioResource.AudioStream, soundSpecifier, audioParams);

            if (ev.Kind == TTSKind.Preview)
                _previewStream = stream?.Entity;
        }
        finally
        {
            ContentRoot.RemoveFile(filePath);
        }
    }

    private float GetRadioGain(ProtoId<RadioChannelPrototype>? channel)
    {
        if (!_radioEnabled)
            return 0f;

        if (channel is not { } id)
            return _radioVolume;

        return _channelVolumes.TryGetValue(id, out var channelVolume)
            ? _radioVolume * channelVolume
            : _radioVolume;
    }

    private static float AdjustVolume(float gain, bool isWhisper)
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(gain);

        if (isWhisper)
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);

        return volume;
    }

    private static float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }
}
