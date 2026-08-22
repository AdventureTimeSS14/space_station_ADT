using Content.Client.Audio;
using Content.Client.Gameplay;
using Content.Shared.ADT.BossMusic;
using Content.Shared.ADT.CCVar;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.BossMusic;

public sealed class ADTBossMusicSystem : EntitySystem
{
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;
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
    private float _volumeSlider;

    private float _duck = 1f;
    private float _duckTarget = 1f;
    private float _duckSpeed;

    private bool _ducking;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        UpdatesAfter.Add(typeof(AudioSystem));

        Subs.CVar(_cfg, ADTCCVars.BossMusicEnabled, OnEnabledChanged, true);
        Subs.CVar(_cfg, ADTCCVars.BossMusicVolume, OnVolumeChanged, true);

        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _stream = null;
        _fadingOut = null;
        _playing = null;

        _duck = 1f;
        _duckTarget = 1f;
        _duckSpeed = 0f;

        if (_ducking)
        {
            ApplyDuck(1f);
            _ducking = false;
        }
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
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        UpdateDuck(frameTime);

        if (!_ducking)
            return;

        ApplyDuck(_duck);

        if (_duck >= 1f)
            _ducking = false;
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

        SetDuckTarget(GetDuck(proto), proto.FadeIn);
    }

    private void Stop(bool fade)
    {
        var fadeOut = 0f;

        if (_playing is { } proto)
            fadeOut = proto.FadeOut;

        if (_stream != null && fade && fadeOut > 0f)
        {
            _contentAudio.FadeOut(_stream, duration: fadeOut);
            _fadingOut = _stream;
        }
        else if (_stream != null)
        {
            _audio.Stop(_stream);
            fadeOut = 0f;
        }

        _stream = null;
        _playing = null;

        SetDuckTarget(1f, fadeOut);
    }

    private void SetDuckTarget(float target, float fade)
    {
        _duckTarget = Math.Clamp(target, 0.01f, 1f);

        if (fade <= 0f)
        {
            _duck = _duckTarget;
            _duckSpeed = 0f;
            return;
        }

        _duckSpeed = MathF.Abs(_duck - _duckTarget) / fade;
    }

    private void UpdateDuck(float frameTime)
    {
        if (_duckSpeed <= 0f)
        {
            _duck = _duckTarget;
        }
        else if (_duck < _duckTarget)
        {
            _duck = MathF.Min(_duckTarget, _duck + _duckSpeed * frameTime);
        }
        else if (_duck > _duckTarget)
        {
            _duck = MathF.Max(_duckTarget, _duck - _duckSpeed * frameTime);
        }

        if (_duck < 1f)
            _ducking = true;
    }

    private void ApplyDuck(float duck)
    {
        var query = AllEntityQuery<AudioComponent>();

        while (query.MoveNext(out var uid, out var audio))
        {
            if (uid == _stream || uid == _fadingOut)
                continue;

            if (audio.Gain <= 0f)
                continue;

            var gain = SharedAudioSystem.VolumeToGain(audio.Params.Volume) * duck;

            if (float.IsNaN(gain))
                continue;

            audio.Gain = gain;
        }
    }

    private float GetVolume(ADTBossMusicPrototype proto)
    {
        return proto.Sound.Params.Volume + SharedAudioSystem.GainToVolume(_volumeSlider);
    }

    private float GetDuck(ADTBossMusicPrototype proto)
    {
        return Math.Clamp(proto.Duck, 0.01f, 1f);
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
}
