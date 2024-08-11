using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using System.Threading.Tasks;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using Robust.Client.Player;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Timing;
using Content.Shared.Corvax.CCCVars;
using Robust.Client.Audio.Effects;
using Robust.Shared.Audio.Effects;

namespace Content.Client.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : SharedSpeechBarksSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(ADTCCVars.BarksVolume, OnVolumeChanged, true);

        SubscribeNetworkEvent<PlaySpeechBarksEvent>(OnEntitySpoke);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(ADTCCVars.BarksVolume, OnVolumeChanged);
    }

    private readonly List<string> _sampleText =
    new()
    {
            "Тест мессЭдж 1.",
            "Тест мессЭдж 2!",
            "Тест мессЭдж 3?",
            "Здесь был котя."
    };

    private const float MinimalVolume = -10f;
    private float _volume = 0.0f;
    private const float WhisperFade = 4f;

    private void OnVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private float AdjustVolume(bool isWhisper)
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }

        return volume;
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }

    private async void OnEntitySpoke(PlaySpeechBarksEvent ev)
    {
        if (_cfg.GetCVar(ADTCCVars.ReplaceTTSWithBarks) == false)
            return;

        if (ev.Message == null)
            return;

        if (ev.Source != null)
        {
            var audioParams = AudioParams.Default
                .WithVolume(AdjustVolume(ev.IsWhisper))
                .WithMaxDistance(AdjustDistance(ev.IsWhisper))
                .WithPlayOffset(0f)
                .WithReferenceDistance(100f);

            if (ev.Message.EndsWith('!'))
                audioParams = audioParams.WithVolume(audioParams.Volume * 1.2f);

            var audioResource = new AudioResource();
            string str = ev.Sound;

            var path = new ResPath(str);
            audioResource.Load(IoCManager.Instance!, path);

            var count = (int)ev.Message.Length / 3f;

            for (var i = 0; i < count; i++)
            {
                if (_player.LocalSession == null)
                    break;
                if (Deleted(GetEntity(ev.Source.Value)) || Terminating(GetEntity(ev.Source.Value)))
                    continue;
                if (Transform(GetEntity(ev.Source.Value)).Coordinates.TryDistance(EntityManager, Transform(_player.LocalEntity ?? EntityUid.Invalid).Coordinates, out var distance) &&
                    distance > SharedChatSystem.VoiceRange)
                    continue;

                _audio.PlayEntity(audioResource.AudioStream, GetEntity(ev.Source.Value), audioParams.WithPitchScale(_random.NextFloat(ev.Pitch - 0.1f, ev.Pitch + 0.1f)));

                await Task.Delay(TimeSpan.FromSeconds(_random.NextFloat(ev.LowVar, ev.HighVar)));
            }

        }
    }

    public async void PlayDataPrewiew(string protoId, float pitch, float lowVar, float highVar)
    {
        if (!_proto.TryIndex<BarkPrototype>(protoId, out var proto))
            return;

        var message = _random.Pick(_sampleText);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(false));

        var count = (int)message.Length / 3f;
        var audioResource = new AudioResource();
        string str = proto.Sound;

        if (message.EndsWith('!'))
            audioParams = audioParams.WithVolume(audioParams.Volume * 1.2f);

        var path = new ResPath(str);
        audioResource.Load(IoCManager.Instance!, path);

        for (var i = 0; i < count; i++)
        {
            if (_player.LocalSession == null)
                break;

            _audio.PlayGlobal(str, _player.LocalSession, audioParams.WithPitchScale(_random.NextFloat(pitch - 0.1f, pitch + 0.1f)));

            await Task.Delay(TimeSpan.FromSeconds(_random.NextFloat(lowVar, highVar)));
        }
    }
}
