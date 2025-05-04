using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Server.Speech;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.SignalingLoudspeak;

public sealed class SignalingLoudspeakSystem : EntitySystem
{
    private HashSet<(string, EntityUid, RadioChannelPrototype)> _recentlySent = new();

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalingLoudspeakComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
        SubscribeLocalEvent<SignalingLoudspeakComponent, ListenEvent>(OnListen);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    private void OnAltVerbs(EntityUid uid, SignalingLoudspeakComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png"));
        var iconPlay = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/group.svg.192dpi.png"));

        // Выбор длинного сигнала
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => SelectSoundMode(uid, component, SelectiveSignaling.Long),
            Text = Loc.GetString("verb-selector-long"),
            Icon = icon
        });

        // Выбор короткого сигнала
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => SelectSoundMode(uid, component, SelectiveSignaling.Short),
            Text = Loc.GetString("verb-selector-short"),
            Icon = icon
        });

        // Включение/выключение воспроизведения
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => ToggleSound(uid, component),
            Text = Loc.GetString(component.PlayingStream != null ? "verb-toggle-off" : "verb-toggle-on"),
            Icon = iconPlay
        });
    }

    private void SelectSoundMode(EntityUid uid, SignalingLoudspeakComponent component, SelectiveSignaling select)
    {
        if (component.SelectedModeSound == select)
            return;

        component.SelectedModeSound = select;

        if (component.PlayingStream != null)
        {
            StopCurrentSound(component);
            PlaySelectedSound(uid, component);
        }
    }

    private void ToggleSound(EntityUid uid, SignalingLoudspeakComponent component)
    {
        if (component.PlayingStream != null)
        {
            StopCurrentSound(component);
            component.MicrophoneEnabled = false;
        }
        else
        {
            PlaySelectedSound(uid, component);
            component.MicrophoneEnabled = true;
        }
    }

    private void StopCurrentSound(SignalingLoudspeakComponent component)
    {
        if (component.PlayingStream != null)
        {
            _audio.Stop(component.PlayingStream.Value);
            component.PlayingStream = null;
        }
    }

    private void PlaySelectedSound(EntityUid uid, SignalingLoudspeakComponent component)
    {
        var sound = component.SelectedModeSound switch
        {
            SelectiveSignaling.Long => component.SoundLong,
            SelectiveSignaling.Short => component.SoundShort,
            _ => null
        };

        if (sound == null)
            return;

        var stream = _audio.PlayPvs(
            sound,
            uid,
            AudioParams.Default
                .WithLoop(true)
                .WithMaxDistance(component.AudioMaxDistance)
                .WithVolume(component.AudioVolume)
        );

        component.PlayingStream = stream?.Entity;
    }

    private void OnListen(EntityUid uid, SignalingLoudspeakComponent component, ListenEvent args)
    {
        // Prevent feedback loops from radio speakers
        if (HasComp<RadioSpeakerComponent>(args.Source))
            return;

        // Check if microphone is disabled via signaling component
        if (!component.MicrophoneEnabled)
            return;

        // Validate broadcast channel
        if (!_protoMan.TryIndex<RadioChannelPrototype>(component.BroadcastChannel, out var channel))
        {
            Log.Warning($"Radio microphone {uid} has invalid channel {component.BroadcastChannel}");
            return;
        }

        // Anti-spam check and message sending
        if (_recentlySent.Add((args.Message, args.Source, channel)))
        {
            _chatSystem.TrySendInGameICMessage(
                uid,
                args.Message,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                checkRadioPrefix: true);

            _audio.PlayPvs(
            component.SoundSpeak,
            uid,
            AudioParams.Default
                .WithLoop(false)
                .WithMaxDistance(component.AudioMaxDistance)
                .WithVolume(component.AudioVolume)
            );
        }
    }
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
