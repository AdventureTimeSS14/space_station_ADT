using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.ADT.SignalingLoudspeak;

public sealed class SignalingLoudspeakSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalingLoudspeakComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
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
        }
        else
        {
            PlaySelectedSound(uid, component);
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
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
