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
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationRadioReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        if (comp.SoundEntity != null && args.Powered)
            _audio.SetGain(comp.SoundEntity, comp.Active ? 1f : 0f);
        else if (comp.SoundEntity != null)
            _audio.SetGain(comp.SoundEntity, 0);
    }

    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;
        if (comp.SoundEntity != null && _power.IsPowered(uid))
            _audio.SetGain(comp.SoundEntity, comp.Active ? 1f : 0f);
    }

    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaPlayedEvent args)
    {
        if (comp.SelectedChannelId != args.ChannelId)
            return;

        var gain = comp.Active ? 3f : 0f;
        var audio = _audio.PlayPredicted(args.MediaPlayed, uid, uid, AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f));
        if (audio != null && _power.IsPowered(uid))
        {
            comp.SoundEntity = audio.Value.Entity;
            _audio.SetGain(comp.SoundEntity, gain);
        }
        else if (audio != null && !_power.IsPowered(uid))
        {
            comp.SoundEntity = audio.Value.Entity;
            _audio.SetGain(comp.SoundEntity, 0);
        }
    }

    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaStoppedEvent args)
    {
        if (comp.SelectedChannelId != args.ChannelId)
            return;

        if (comp.SoundEntity != null)
            comp.SoundEntity = _audio.Stop(comp.SoundEntity);
    }

    private void OnGetVerbs(EntityUid uid, StationRadioReceiverComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var allowedChannels = new[] { "ADTOldBroadcast", "ADTOldBroadcast2", "ADTOldBroadcast3" };

        foreach (var id in allowedChannels)
        {
            if (!_proto.TryIndex<RadioChannelPrototype>(id, out var channel))
                continue;

            var channelId = channel.ID; // Capture in closure
            var verb = new Verb
            {
                Category = VerbCategory.StationRadio,
                Text = channel.LocalizedName,
                Act = () =>
                {
                    if (comp.SelectedChannelId == channelId)
                        return;

                    if (comp.SoundEntity != null)
                        comp.SoundEntity = _audio.Stop(comp.SoundEntity);

                    comp.SelectedChannelId = channelId;
                    Dirty(uid, comp);
                }
            };
            args.Verbs.Add(verb);
        }
    }
}