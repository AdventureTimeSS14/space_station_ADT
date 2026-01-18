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
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.Radio.Components;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class StationRadioReceiverSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationRadioBroadcastSystem _broadcastSystem = default!;

    private readonly Dictionary<EntityUid, (Guid BroadcastId, TimeSpan LastCall)> _eventTracker = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioReceiverComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StationRadioReceiverComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<StationRadioReceiverComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioChannelChangedEvent>(OnChannelChanged);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationRadioReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetState(EntityUid uid, StationRadioReceiverComponent comp, ref ComponentGetState args)
    {
        args.State = new StationRadioReceiverComponentState(
            comp.Active,
            comp.SelectedChannelId,
            comp.CurrentMedia,
            comp.StartTime,
            comp.CurrentTrackId);
    }

    private void OnHandleState(EntityUid uid, StationRadioReceiverComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not StationRadioReceiverComponentState state)
            return;

        var mediaChanged = comp.CurrentMedia != state.CurrentMedia ||
                          comp.StartTime != state.StartTime ||
                          comp.CurrentTrackId != state.CurrentTrackId;

        comp.Active = state.Active;
        comp.SelectedChannelId = state.SelectedChannelId;
        comp.CurrentMedia = state.CurrentMedia;
        comp.StartTime = state.StartTime;
        comp.CurrentTrackId = state.CurrentTrackId;

        if (_netManager.IsClient && mediaChanged && comp.CurrentMedia != null && comp.StartTime != null)
        {
            PlayMediaSynchronized(uid, comp, comp.CurrentMedia, comp.StartTime.Value, comp.CurrentTrackId);
        }
        else if (_netManager.IsClient && comp.CurrentMedia == null && comp.SoundEntity.HasValue)
        {
            StopMedia(uid, comp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        CleanupEventTracker();
    }

    private void CleanupEventTracker()
    {
        var currentTime = _gameTiming.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (uid, (_, lastCall)) in _eventTracker)
        {
            if (currentTime - lastCall > TimeSpan.FromSeconds(1))
                toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
            _eventTracker.Remove(uid);
    }

    private void OnShutdown(EntityUid uid, StationRadioReceiverComponent comp, ComponentShutdown args)
    {
        if (comp.SelectedChannelId != null)
        {
            _broadcastSystem.UnsubscribeFromChannel(uid, comp.SelectedChannelId);
        }

        StopMedia(uid, comp);
    }

    private void OnChannelChanged(EntityUid uid, StationRadioReceiverComponent comp, StationRadioChannelChangedEvent args)
    {
        if (comp.SelectedChannelId == args.NewChannelId)
            return;

        StopMedia(uid, comp);

        _broadcastSystem.SubscribeToChannel(uid, args.NewChannelId, comp);

        comp.SelectedChannelId = args.NewChannelId;
        Dirty(uid, comp);

        var broadcast = _broadcastSystem.GetCurrentBroadcast(args.NewChannelId);
        if (broadcast != null)
        {
            comp.CurrentMedia = broadcast.Media;
            comp.StartTime = broadcast.StartTime;
            comp.CurrentTrackId = broadcast.BroadcastId;

            if (comp.Active && _power.IsPowered(uid))
            {
                PlayMediaSynchronized(uid, comp, broadcast.Media, broadcast.StartTime, broadcast.BroadcastId);
            }
        }
        else
        {
            comp.CurrentMedia = null;
            comp.StartTime = null;
            comp.CurrentTrackId = null;
        }
    }

    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value) &&
            TryComp<AudioComponent>(comp.SoundEntity.Value, out var audioComp))
        {
            var gain = args.Powered ? (comp.Active ? 1f : 0f) : 0f;
            _audio.SetGain(comp.SoundEntity.Value, gain, audioComp);
        }

        if (!args.Powered)
        {
            StopMedia(uid, comp);
        }
        else if (args.Powered && comp.Active && comp.SelectedChannelId != null)
        {
            var broadcast = _broadcastSystem.GetCurrentBroadcast(comp.SelectedChannelId);
            if (broadcast != null)
            {
                var ev = new StationRadioMediaPlayedEvent(
                    broadcast.Media,
                    comp.SelectedChannelId,
                    broadcast.StartTime,
                    broadcast.BroadcastId);
                RaiseLocalEvent(uid, ev);
            }
        }
    }

    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;

        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value) &&
            TryComp<AudioComponent>(comp.SoundEntity.Value, out var audioComp) && _power.IsPowered(uid))
        {
            var gain = comp.Active ? 1f : 0f;
            _audio.SetGain(comp.SoundEntity.Value, gain, audioComp);
        }

        if (!comp.Active)
        {
            StopMedia(uid, comp);
        }
        else if (comp.Active && comp.SelectedChannelId != null && _power.IsPowered(uid))
        {
            var broadcast = _broadcastSystem.GetCurrentBroadcast(comp.SelectedChannelId);
            if (broadcast != null)
            {
                var ev = new StationRadioMediaPlayedEvent(
                    broadcast.Media,
                    comp.SelectedChannelId,
                    broadcast.StartTime,
                    broadcast.BroadcastId);
                RaiseLocalEvent(uid, ev);
            }
        }

        Dirty(uid, comp);
    }

    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaPlayedEvent args)
    {
        if (_eventTracker.TryGetValue(uid, out var tracked) &&
            tracked.BroadcastId == args.BroadcastId &&
            _gameTiming.CurTime - tracked.LastCall < TimeSpan.FromMilliseconds(500))
        {
            return;
        }

        _eventTracker[uid] = (args.BroadcastId, _gameTiming.CurTime);

        if (comp.SelectedChannelId != args.ChannelId)
            return;

        if (comp.CurrentTrackId == args.BroadcastId && comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value))
            return;

        comp.CurrentMedia = args.MediaPlayed;
        comp.StartTime = args.StartTime;
        comp.CurrentTrackId = args.BroadcastId;

        if (_netManager.IsServer)
        {
            Dirty(uid, comp);
        }

        if (comp.Active && _power.IsPowered(uid))
        {
            PlayMediaSynchronized(uid, comp, args.MediaPlayed, args.StartTime, args.BroadcastId);
        }
    }

    private void PlayMediaSynchronized(EntityUid uid, StationRadioReceiverComponent comp,
        SoundPathSpecifier media, TimeSpan startTime, Guid? broadcastId)
    {
        StopMedia(uid, comp);

        if (!comp.Active || !_power.IsPowered(uid))
            return;

        var audioParams = AudioParams.Default
            .WithVolume(3f)
            .WithMaxDistance(4.5f)
            .WithLoop(true)
            .WithVariation(0f);

        var audio = _audio.PlayPredicted(media, uid, null, audioParams);
        if (audio == null)
            return;

        comp.SoundEntity = audio.Value.Entity;

        var elapsed = _gameTiming.CurTime - startTime;
        if (elapsed > TimeSpan.Zero)
        {
            var elapsedSeconds = (float)elapsed.TotalSeconds;
            _audio.SetPlaybackPosition(audio.Value.Entity, elapsedSeconds);
        }
    }

    private void StopMedia(EntityUid uid, StationRadioReceiverComponent comp)
    {
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value))
        {
            _audio.Stop(comp.SoundEntity.Value);
        }

        comp.SoundEntity = null;
    }

    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaStoppedEvent args)
    {
        if (comp.SelectedChannelId != args.ChannelId)
            return;

        StopMedia(uid, comp);
        comp.CurrentMedia = null;
        comp.StartTime = null;
        comp.CurrentTrackId = null;

        if (_netManager.IsServer)
        {
            Dirty(uid, comp);
        }
    }

    private void OnGetVerbs(EntityUid uid, StationRadioReceiverComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasComp<VerbSelectableRadioChannelComponent>(uid))
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

                    var ev = new StationRadioChannelChangedEvent(channelId);
                    RaiseLocalEvent(uid, ev);
                }
            };
            args.Verbs.Add(verb);
        }
    }
}

[Serializable, NetSerializable]
public sealed class StationRadioReceiverComponentState : ComponentState
{
    public bool Active { get; }
    public string? SelectedChannelId { get; }
    public SoundPathSpecifier? CurrentMedia { get; }
    public TimeSpan? StartTime { get; }
    public Guid? CurrentTrackId { get; }

    public StationRadioReceiverComponentState(bool active, string? selectedChannelId,
        SoundPathSpecifier? currentMedia, TimeSpan? startTime, Guid? currentTrackId)
    {
        Active = active;
        SelectedChannelId = selectedChannelId;
        CurrentMedia = currentMedia;
        StartTime = startTime;
        CurrentTrackId = currentTrackId;
    }
}