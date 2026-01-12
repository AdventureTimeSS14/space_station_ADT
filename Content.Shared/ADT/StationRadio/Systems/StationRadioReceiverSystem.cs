using Content.Shared.ADT.StationRadio;
using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class StationRadioReceiverSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationRadioReceiverComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationRadioReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnStartup(EntityUid uid, StationRadioReceiverComponent comp, ComponentStartup args)
    {
        if (comp.SelectedChannelId == null)
        {
            comp.SelectedChannelId = RadioConstants.DefaultChannel;
            Dirty(uid, comp);
        }

        TryJoinCurrentBroadcast(uid, comp);
    }

    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        if (comp.SoundEntity != null)
        {
            var gain = args.Powered ? (comp.Active ? 1f : 0f) : 0f;
            _audio.SetGain(comp.SoundEntity.Value, gain);
        }

        if (args.Powered)
            TryJoinCurrentBroadcast(uid, comp);
    }

    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;

        if (comp.SoundEntity != null && _power.IsPowered(uid))
            _audio.SetGain(comp.SoundEntity.Value, comp.Active ? 1f : 0f);
    }

    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaPlayedEvent args)
    {
        if (comp.SelectedChannelId != args.ChannelId)
            return;

        PlayMedia(uid, comp, args.MediaPlayed);
    }

    private void PlayMedia(EntityUid uid, StationRadioReceiverComponent comp, SoundPathSpecifier media)
    {
        // Защита от дублей: если уже играет точно то же — пропускаем полностью
        if (comp.CurrentMedia == media && comp.SoundEntity.HasValue)
            return;

        // Стоп старого (на всякий)
        if (comp.SoundEntity.HasValue)
        {
            _audio.Stop(comp.SoundEntity.Value);
            comp.SoundEntity = null;
        }

        comp.CurrentMedia = null; // сбрасываем временно

        if (!comp.Active || !_power.IsPowered(uid))
            return;

        var audioParams = AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f).WithLoop(true);
        var audio = _audio.PlayPredicted(media, uid, uid, audioParams);

        if (audio == null)
            return;

        comp.SoundEntity = audio.Value.Entity;
        comp.CurrentMedia = media;
    }

    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaStoppedEvent args)
    {
        if (comp.SelectedChannelId != args.ChannelId)
            return;

        if (comp.SoundEntity != null)
        {
            _audio.Stop(comp.SoundEntity.Value);
            comp.SoundEntity = null;
        }

        comp.CurrentMedia = null;
    }

    private void OnGetVerbs(EntityUid uid, StationRadioReceiverComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        foreach (var id in RadioConstants.AllowedChannels)
        {
            if (!_proto.TryIndex<RadioChannelPrototype>(id, out var channel))
                continue;

            var channelId = channel.ID;
            var verb = new Verb
            {
                Category = VerbCategory.StationRadio,
                Text = channel.LocalizedName,
                Act = () =>
                {
                    if (comp.SelectedChannelId == channelId)
                        return;

                    // Жёсткий стоп старого
                    if (comp.SoundEntity != null)
                    {
                        _audio.Stop(comp.SoundEntity.Value);
                        comp.SoundEntity = null;
                        comp.CurrentMedia = null;
                    }

                    comp.SelectedChannelId = channelId;
                    Dirty(uid, comp);

                    TryJoinCurrentBroadcast(uid, comp);
                }
            };
            args.Verbs.Add(verb);
        }
    }

    private void TryJoinCurrentBroadcast(EntityUid uid, StationRadioReceiverComponent comp)
    {
        if (comp.SelectedChannelId == null || !_power.IsPowered(uid))
            return;

        if (GetCurrentBroadcastMedia(comp.SelectedChannelId) is { } media)
            PlayMedia(uid, comp, media);
    }

    private SoundPathSpecifier? GetCurrentBroadcastMedia(string? channelId)
    {
        if (channelId == null)
            return null;

        var query = EntityQueryEnumerator<StationRadioServerComponent>();
        while (query.MoveNext(out _, out var serverComp))
        {
            if (serverComp.ChannelId == channelId && serverComp.CurrentMedia != null)
                return serverComp.CurrentMedia;
        }

        return null;
    }
}