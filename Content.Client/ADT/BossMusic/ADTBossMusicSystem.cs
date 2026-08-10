using Content.Client.Audio;
using Content.Client.Gameplay;
using Content.Shared.ADT.BossMusic;
using Content.Shared.ADT.CCVar;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.BossMusic;

public sealed class ADTBossMusicSystem : EntitySystem
{
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public const float VolumeMultiplier = 3f;

    private ADTBossMusicPrototype? _playing;

    private EntityUid? _stream;
    private EntityUid? _fadingOut;

    private bool _enabled;

    private float _fadingOutDuck = 1f;
    private float _volumeSlider;
    private float _masterVolume = 1f;
    private float _duck = 1f;
    private float _appliedGain = -1f;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        Subs.CVar(_cfg, ADTCCVars.BossMusicEnabled, OnEnabledChanged, true);
        Subs.CVar(_cfg, ADTCCVars.BossMusicVolume, OnVolumeChanged, true);
        Subs.CVar(_cfg, CVars.AudioMasterVolume, OnMasterVolumeChanged, true);

        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _stream = null;
        _fadingOut = null;
        _playing = null;
        UpdateDuck();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_fadingOut != null && !Exists(_fadingOut))
            _fadingOut = null;

        if (_playing != null && !Exists(_stream))
        {
            _stream = null;
            Stop(fade: false);
        }

        var desired = GetDesiredMusic();

        if (desired != _playing)
        {
            if (_playing != null)
                Stop(fade: true);

            if (desired != null)
                Play(desired);
        }

        UpdateDuck();
    }

    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent args)
    {
        if (_playing == null)
            return;

        args.Cancelled = true;
    }

    private ADTBossMusicPrototype? GetDesiredMusic()
    {
        if (!_enabled || _volumeSlider <= 0f)
            return null;

        if (_state.CurrentState is not GameplayState)
            return null;

        if (_player.LocalEntity is not { } player)
            return null;

        if (!TryComp<ADTBossMusicListenerComponent>(player, out var listener))
            return null;

        if (!_proto.TryIndex(listener.Music, out var proto))
            return null;

        return proto;
    }

    private void Play(ADTBossMusicPrototype proto)
    {
        var stream = _audio.PlayGlobal(
            _audio.ResolveSound(proto.Sound),
            Filter.Local(),
            false,
            AudioParams.Default.WithLoop(true).WithVolume(GetVolume(proto)));

        if (stream == null)
            return;

        _playing = proto;
        _stream = stream.Value.Entity;

        if (proto.FadeIn > 0f)
            _contentAudio.FadeIn(_stream, stream.Value.Component, proto.FadeIn);

        _contentAudio.DisableAmbientMusic();
    }

    private void Stop(bool fade)
    {
        if (_stream != null && fade && _playing is { FadeOut: > 0f } proto)
        {
            _contentAudio.FadeOut(_stream, duration: proto.FadeOut);

            _fadingOut = _stream;
            _fadingOutDuck = GetDuck(proto);
        }
        else if (_stream != null)
        {
            _audio.Stop(_stream);
        }

        _stream = null;
        _playing = null;
    }

    private void UpdateDuck()
    {
        var duck = 1f;

        if (_playing != null)
            duck = GetDuck(_playing);
        else if (_fadingOut != null)
            duck = _fadingOutDuck;

        _duck = duck;

        ApplyGain(force: duck < 1f);
    }

    private void ApplyGain(bool force)
    {
        var gain = _masterVolume * _duck;
        var changed = !MathHelper.CloseTo(_appliedGain, gain);

        if (!changed && !force)
            return;

        //if (changed)
        //    Log.Info($"Приглушение музыкой босса: duck {_duck:0.00}, мастер-громкость {_appliedGain:0.00} -> {gain:0.00}");

        _appliedGain = gain;
        _audioManager.SetMasterGain(gain);
    }

    private float GetVolume(ADTBossMusicPrototype proto)
    {
        var gain = Math.Clamp(GetNominalGain(proto) / GetDuck(proto), 0.0001f, 1f);

        return SharedAudioSystem.GainToVolume(gain);
    }

    private float GetNominalGain(ADTBossMusicPrototype proto)
    {
        var gain = SharedAudioSystem.VolumeToGain(proto.Sound.Params.Volume) * _volumeSlider;

        return Math.Clamp(gain, 0f, 1f);
    }

    private float GetDuck(ADTBossMusicPrototype proto)
    {
        var duck = Math.Clamp(proto.Duck, 0.01f, 1f);

        return Math.Max(duck, GetNominalGain(proto));
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;
    }

    private void OnVolumeChanged(float value)
    {
        _volumeSlider = value;

        if (_playing == null || _stream == null)
            return;

        _audio.SetVolume(_stream, GetVolume(_playing));
    }

    private void OnMasterVolumeChanged(float value)
    {
        _masterVolume = value;

        ApplyGain(force: _duck < 1f);
    }
}
