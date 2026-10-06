using System.IO;
using Content.Shared.ADT.MusicRecorder;
using Content.Shared.Audio.Jukebox;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.MusicRecorder;

public sealed class MusicCassettePlaybackSystem : EntitySystem
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedJukeboxSystem _jukebox = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<string, LoadedTrack> _cache = new();
    private readonly Dictionary<string, PendingTrack> _pending = new();
    private readonly Dictionary<string, TimeSpan> _requestedAt = new();
    private readonly Dictionary<EntityUid, LocalPlayback> _playing = new();
    private readonly List<EntityUid> _stopScratch = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnJukeboxShutdown);
        SubscribeNetworkEvent<MusicCassetteAudioChunkEvent>(OnAudioChunk);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public bool TryGetPlayback(EntityUid jukebox, out EntityUid audio)
    {
        if (_playing.TryGetValue(jukebox, out var playback) && Exists(playback.Audio))
        {
            audio = playback.Audio;
            return true;
        }

        audio = default;
        return false;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stopScratch.Clear();
        foreach (var (uid, _) in _playing)
            _stopScratch.Add(uid);

        foreach (var uid in _stopScratch)
            Stop(uid, force: true);

        _playing.Clear();
        _pending.Clear();
        _requestedAt.Clear();

        foreach (var (_, loaded) in _cache)
        {
            loaded.Resource.AudioStream.Dispose();
        }

        _cache.Clear();
    }

    private void OnJukeboxShutdown(Entity<JukeboxComponent> ent, ref ComponentShutdown args)
    {
        Stop(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (string.IsNullOrEmpty(comp.CustomTrackId) && !_playing.ContainsKey(uid))
                continue;

            Sync((uid, comp));
        }

        // Component gone without a shutdown, or state already cleared the track.
        _stopScratch.Clear();
        foreach (var (uid, playback) in _playing)
        {
            if (!TryComp<JukeboxComponent>(uid, out var comp) ||
                comp.CustomTrackId != playback.TrackId ||
                !comp.CustomPlaying)
            {
                _stopScratch.Add(uid);
            }
        }

        foreach (var uid in _stopScratch)
        {
            Stop(uid);
        }
    }

    private void OnAudioChunk(MusicCassetteAudioChunkEvent ev)
    {
        if (ev.Total <= 0 || ev.Total > MusicCassetteComponent.MaxTrackBytes || ev.Data.Length == 0)
            return;

        if (!_pending.TryGetValue(ev.TrackId, out var pending))
        {
            pending = new PendingTrack
            {
                Total = ev.Total,
                Buffer = new byte[ev.Total],
            };
            _pending[ev.TrackId] = pending;
        }

        if (ev.Offset < 0 || ev.Offset + ev.Data.Length > pending.Buffer.Length || pending.Filled.Contains(ev.Offset))
            return;

        ev.Data.CopyTo(pending.Buffer, ev.Offset);
        pending.Filled.Add(ev.Offset);
        pending.Received += ev.Data.Length;

        if (pending.Received < pending.Total)
            return;

        _pending.Remove(ev.TrackId);
        _requestedAt.Remove(ev.TrackId);

        if (!TryLoad(ev.TrackId, pending.Buffer))
            return;

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CustomTrackId == ev.TrackId)
                Sync((uid, comp));
        }
    }

    private void Sync(Entity<JukeboxComponent> ent)
    {
        var trackId = ent.Comp.CustomTrackId;
        if (string.IsNullOrEmpty(trackId))
        {
            Stop(ent.Owner);
            return;
        }

        if (!_cache.ContainsKey(trackId))
            Request(ent, trackId);

        if (!ent.Comp.CustomPlaying)
        {
            Stop(ent.Owner);
            return;
        }

        if (!_cache.TryGetValue(trackId, out var loaded))
            return;

        var position = GetPosition(ent.Comp);
        if (_playing.TryGetValue(ent.Owner, out var playback) &&
            playback.TrackId == trackId &&
            Exists(playback.Audio))
        {
            ApplyLive(ent.Comp, playback.Audio, position);
            return;
        }

        Stop(ent.Owner);
        Start(ent, loaded, position);
    }

    private void Request(Entity<JukeboxComponent> ent, string trackId)
    {
        if (_requestedAt.TryGetValue(trackId, out var requested) && _timing.CurTime - requested < TimeSpan.FromSeconds(2))
            return;

        var disk = _jukebox.GetInsertedDisk(ent);
        if (disk == null)
            return;

        _requestedAt[trackId] = _timing.CurTime;
        RaiseNetworkEvent(new MusicCassetteRequestAudioEvent(GetNetEntity(disk.Value), trackId));
    }

    private bool TryLoad(string trackId, byte[] bytes)
    {
        if (!MusicOgg.IsMonoVorbis(bytes))
            return false;

        try
        {
            var stream = _audioManager.LoadAudioOggVorbis(new MemoryStream(bytes), trackId);
            if (stream.ChannelCount != 1)
            {
                stream.Dispose();
                return false;
            }

            var resource = new AudioResource(stream);
            var path = new ResPath($"/Audio/ADT/MusicRecorder/{trackId}.ogg");
            _resources.CacheResource(path, resource);
            _cache[trackId] = new LoadedTrack(resource, path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void Start(Entity<JukeboxComponent> ent, LoadedTrack loaded, float position)
    {
        var audioParams = AudioParams.Default
            .WithMaxDistance(10f)
            .WithVolume(GetVolume(ent.Comp))
            .WithPlayOffset(position);

        var playing = _audio.PlayEntity(loaded.Resource.AudioStream, ent.Owner, new ResolvedPathSpecifier(loaded.Path), audioParams);
        if (playing == null)
            return;

        _playing[ent.Owner] = new LocalPlayback(ent.Comp.CustomTrackId!, playing.Value.Entity);
        NotifyUi(ent.Owner);
    }

    private void ApplyLive(JukeboxComponent comp, EntityUid audio, float position)
    {
        _audio.SetVolume(audio, GetVolume(comp));

        if (!TryComp<AudioComponent>(audio, out var audioComp))
            return;

        if (!audioComp.Playing)
            _audio.SetState(audio, AudioState.Playing);

        if (Math.Abs(audioComp.PlaybackPosition - position) > 0.45f)
            _audio.SetPlaybackPosition(audio, position);
    }

    private float GetPosition(JukeboxComponent comp)
    {
        if (!comp.CustomPlaying)
            return comp.CustomOffset;

        return comp.CustomOffset + (float) (_timing.CurTime - comp.CustomStartedAt).TotalSeconds;
    }

    private float GetVolume(JukeboxComponent comp)
    {
        var volume = SharedJukeboxSystem.MapToRange(comp.Volume, comp.MinSlider, comp.MaxSlider, comp.MinVolume, comp.MaxVolume);
        return volume + SharedAudioSystem.GainToVolume(_cfg.GetCVar(CCVars.AmbientMusicVolume));
    }

    private void Stop(EntityUid jukebox, bool force = false)
    {
        if (!_playing.TryGetValue(jukebox, out var playback))
            return;

        // AudioSystem.Stop no-ops outside the first predicted tick. Keep the entry and retry,
        // otherwise the local source is forgotten while it keeps playing.
        if (!force && !_timing.IsFirstTimePredicted)
            return;

        _playing.Remove(jukebox);

        if (!Exists(playback.Audio))
            return;

        _audio.SetState(playback.Audio, AudioState.Stopped);
        QueueDel(playback.Audio);
    }

    private void NotifyUi(EntityUid jukebox)
    {
        if (_ui.TryGetOpenUi<Content.Client.Audio.Jukebox.JukeboxBoundUserInterface>(jukebox, JukeboxUiKey.Key, out var bui))
            bui.Reload();
    }

    private sealed class PendingTrack
    {
        public int Total;
        public int Received;
        public byte[] Buffer = Array.Empty<byte>();
        public HashSet<int> Filled = new();
    }

    private readonly record struct LoadedTrack(AudioResource Resource, ResPath Path);

    private readonly record struct LocalPlayback(string TrackId, EntityUid Audio);
}
