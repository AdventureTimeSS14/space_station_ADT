using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; // ADT-Tweak

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetVolumeMessage>(OnJukeboxSetVolume); // ADT-Tweak
        SubscribeLocalEvent<JukeboxComponent, JukeboxToggleLoopMessage>(OnJukeboxToggleLoop); // ADT-Tweak
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
            // ADT-Tweak start
            if (component.PlaybackStartTime == null && component.CurrentPlaybackOffset > 0)
            {
                Audio.SetPlaybackPosition(component.AudioStream, component.CurrentPlaybackOffset);
            }
            component.PlaybackStartTime = _gameTiming.CurTime;
            Dirty(uid, component);
            // ADT-Tweak end
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);
            PlayTrack(uid, component); // ADT-Tweak
        }
    }

    // ADT-Tweak start
    private void PlayTrack(EntityUid uid, JukeboxComponent component)
    {
        if (string.IsNullOrEmpty(component.SelectedSongId) ||
            !_protoManager.Resolve(component.SelectedSongId, out var jukeboxProto))
        {
            return;
        }

        var audioParams = AudioParams.Default
            .WithMaxDistance(10f)
            .WithVolume(MapToRange(component.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume))
            .WithPlayOffset(component.CurrentPlaybackOffset);

        component.AudioStream = Audio.PlayPvs(jukeboxProto.Path, uid, audioParams)?.Entity;
        component.PlaybackStartTime = _gameTiming.CurTime;
        component.CurrentPlaybackOffset = 0f;
        Dirty(uid, component);
    }
    // ADT-Tweak end

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        // ADT-Tweak start: Validate AudioStream before using
        if (ent.Comp.AudioStream.HasValue && TerminatingOrDeleted(ent.Comp.AudioStream.Value))
        {
            ent.Comp.AudioStream = null;
            Dirty(ent);
            return;
        }

        if (!ent.Comp.AudioStream.HasValue)
            return;
        // ADT-Tweak end

        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);

        // ADT-Tweak start
        if (ent.Comp.PlaybackStartTime.HasValue)
        {
            var elapsed = (float)(_gameTiming.CurTime - ent.Comp.PlaybackStartTime.Value).TotalSeconds;
            ent.Comp.CurrentPlaybackOffset += elapsed;
            ent.Comp.PlaybackStartTime = null;
            Dirty(ent);
        }
        // ADT-Tweak end
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            // ADT-Tweak start: Validate AudioStream before using
            if (component.AudioStream.HasValue && TerminatingOrDeleted(component.AudioStream.Value))
            {
                component.AudioStream = null;
                Dirty(uid, component);
                return;
            }

            if (!component.AudioStream.HasValue)
                return;
            // ADT-Tweak end

            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            // ADT-Tweak start
            var newPosition = args.SongTime + offset;
            Audio.SetPlaybackPosition(component.AudioStream, newPosition);

            component.CurrentPlaybackOffset = newPosition;
            component.PlaybackStartTime = _gameTiming.CurTime;
            Dirty(uid, component);
            // ADT-Tweak end
        }
    }

    /// ADT-Tweak start
    private void OnJukeboxSetVolume(EntityUid uid, JukeboxComponent component, JukeboxSetVolumeMessage args)
    {
        if (component.AudioStream.HasValue && TerminatingOrDeleted(component.AudioStream.Value))
        {
            component.AudioStream = null;
            Dirty(uid, component);
        }

        SetJukeboxVolume(uid, component, args.Volume);

        if (!component.AudioStream.HasValue || TerminatingOrDeleted(component.AudioStream.Value))
            return;

        if (!TryComp<AudioComponent>(component.AudioStream, out var audioComponent))
            return;

        Audio.SetVolume(component.AudioStream, MapToRange(args.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume));
    }

    private void OnJukeboxToggleLoop(EntityUid uid, JukeboxComponent component, JukeboxToggleLoopMessage args)
    {
        ToggleLoop(uid, component);
    }
    /// ADT-Tweak end

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity);
    }

    private void Stop(Entity<JukeboxComponent> entity)
    {
        // ADT-Tweak start: Validate AudioStream before using
        if (entity.Comp.AudioStream.HasValue && !TerminatingOrDeleted(entity.Comp.AudioStream.Value))
        {
            Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
            entity.Comp.AudioStream = null;
        }
        // ADT-Tweak end

        // ADT-Tweak start
        entity.Comp.CurrentPlaybackOffset = 0f;
        entity.Comp.PlaybackStartTime = null;
        // ADT-Tweak end
        Dirty(entity);
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        if (!Audio.IsPlaying(component.AudioStream))
        {
            component.SelectedSongId = args.SongId;
            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
            component.AudioStream = Audio.Stop(component.AudioStream);
        }

        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // ADT-Tweak start: Clean up invalid AudioStream references to prevent PVS errors
            if (comp.AudioStream.HasValue && TerminatingOrDeleted(comp.AudioStream.Value))
            {
                comp.AudioStream = null;
                Dirty(uid, comp);
                continue;
            }
            // ADT-Tweak end

            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;

                    TryUpdateVisualState(uid, comp);
                }
            }

            // ADT-Tweak start
            if (!comp.PlaybackStartTime.HasValue || !comp.SelectedSongId.HasValue || !Exists(comp.AudioStream))
                continue;

            var elapsed = (float)(_gameTiming.CurTime - comp.PlaybackStartTime.Value).TotalSeconds;
            var currentPosition = comp.CurrentPlaybackOffset + elapsed;

            if (!TryComp<AudioComponent>(comp.AudioStream, out var audioComp))
                continue;

            var audioLength = Audio.GetAudioLength(audioComp.FileName).TotalSeconds;

            if (currentPosition < audioLength)
                continue;

            if (comp.LoopEnabled)
            {
                comp.CurrentPlaybackOffset = 0f;
                comp.PlaybackStartTime = _gameTiming.CurTime;
                Audio.SetPlaybackPosition(comp.AudioStream, 0f);
                Audio.SetState(comp.AudioStream, AudioState.Playing);
            }
            else
            {
                comp.AudioStream = Audio.Stop(comp.AudioStream);
                comp.CurrentPlaybackOffset = 0f;
                comp.PlaybackStartTime = null;
            }

            Dirty(uid, comp);
            // ADT-Tweak  end
        }
    }

    /// ADT-Tweak start
    private void SetJukeboxVolume(EntityUid uid, JukeboxComponent component, float volume)
    {
        component.Volume = volume;
        Dirty(uid, component);
    }

    private void ToggleLoop(EntityUid uid, JukeboxComponent component)
    {
        component.LoopEnabled = !component.LoopEnabled;
        Dirty(uid, component);
    }
    /// ADT-Tweak end

    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, finalState);
    }
}
