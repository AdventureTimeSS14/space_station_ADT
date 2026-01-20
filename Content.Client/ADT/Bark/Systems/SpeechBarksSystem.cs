using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using System.Threading.Tasks;
using Robust.Client.Player;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Timing;
using Content.Shared.ADT.Language;
using Robust.Shared.Map;
using Robust.Client.Audio;

namespace Content.Client.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float MinimalVolume = -10f;
    private const float WhisperFade = 4f;
    private float _volume = 0.0f;

    private List<ActiveBark> _activeBarks = new();

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

    private void OnVolumeChanged(float volume)
        => _volume = volume;

    private float AdjustVolume(string message, bool isWhisper)
    {
        var volume = isWhisper ? _volume - WhisperFade : _volume;

        if (message.EndsWith("!"))
            volume += 1.5f;

        return MinimalVolume + SharedAudioSystem.GainToVolume(volume);
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? 5 : 10;
    }

    private void OnEntitySpoke(PlaySpeechBarksEvent ev)
    {
        if (!_cfg.GetCVar(ADTCCVars.ReplaceTTSWithBarks))
            return;

        if (ev.Message == null)
            return;

        if (!TryGetEntity(ev.Source, out var source) || Transform(source.Value).MapID == MapId.Nullspace)
            return;

        var bark = new ActiveBark(source,
                                  ev.SoundSpecifier,
                                  AdjustVolume(ev.Message, ev.IsWhisper),
                                  ev.Pitch,
                                  AdjustDistance(ev.IsWhisper),
                                  (ev.LowVar, ev.HighVar),
                                  ev.Message.Length / 3 + 1);
        _activeBarks.Add(bark);
    }

    public void PlayDataPreview(string protoId, float pitch, float lowVar, float highVar)
    {
        if (!_proto.TryIndex<BarkPrototype>(protoId, out var proto))
            return;

        var bark = new ActiveBark(null,
                                  proto.Sound,
                                  AdjustVolume("Test message", false),
                                  pitch,
                                  AdjustDistance(false),
                                  (lowVar, highVar),
                                  9);
        _activeBarks.Add(bark);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalSession == null)
            return;

        for (var i = _activeBarks.Count - 1; i >= 0; i--)
        {
            var item = _activeBarks[i];

            if (item.NextSound > _timing.CurTime)
                continue;

            if (item.BarksPlayed >= item.Length)
            {
                _activeBarks.Remove(item);
                continue;
            }

            var audioParams = AudioParams.Default.WithPitchScale(_random.NextFloat(item.Pitch - 0.1f, item.Pitch + 0.1f)).WithVolume(item.Volume).WithMaxDistance(item.Distance);
            item.BarksPlayed++;
            item.NextSound = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(item.DelayVariation.Item1, item.DelayVariation.Item2));

            if (item.Source == null)
            {
                if (item.HasSource)
                    _activeBarks.Remove(item);
                else
                    _audio.PlayGlobal(_audio.ResolveSound(item.Sound), _player.LocalSession, audioParams);

                continue;
            }

            if (_player.LocalEntity is { Valid: true } player)
            {
                if (item.Source == _player.LocalEntity)
                    _audio.PlayGlobal(_audio.ResolveSound(item.Sound), player, audioParams);
                else
                    _audio.PlayEntity(_audio.ResolveSound(item.Sound), _player.LocalSession, item.Source.Value, audioParams);
            }
            else
            {
                _activeBarks.Remove(item);
                continue;
            }
        }
    }

    private sealed class ActiveBark
    {
        public readonly EntityUid? Source;
        public readonly SoundSpecifier Sound = default!;
        public readonly float Volume = default!;
        public readonly float Pitch = default!;
        public readonly float Distance = default!;
        public readonly (float, float) DelayVariation = default!;
        public readonly int Length = default!;
        public readonly bool HasSource;

        public TimeSpan NextSound = TimeSpan.Zero;
        public int BarksPlayed = 0;

        public ActiveBark(EntityUid? source, SoundSpecifier sound, float volume, float pitch, float distance, (float, float) delay, int length)
        {
            Source = source;
            HasSource = source.HasValue;
            Sound = sound;
            Volume = volume;
            Pitch = pitch;
            Distance = distance;
            DelayVariation = delay;
            Length = length;
        }
    }
}
