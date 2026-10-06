using Content.Shared.ADT.MusicRecorder;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.MusicRecorder;

public sealed class MusicRecorderSystem : SharedMusicRecorderSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly Dictionary<(EntityUid Recorder, EntityUid Actor), PendingUpload> _uploads = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MusicRecorderComponent, EntInsertedIntoContainerMessage>(OnCassetteInserted);
        SubscribeLocalEvent<MusicRecorderComponent, EntRemovedFromContainerMessage>(OnCassetteRemoved);
        SubscribeLocalEvent<MusicRecorderComponent, AfterActivatableUIOpenEvent>(OnUiOpened);
        SubscribeLocalEvent<MusicRecorderComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<MusicRecorderComponent, MusicRecorderRenameMessage>(OnRename);
        SubscribeLocalEvent<MusicRecorderComponent, MusicRecorderUploadHeaderMessage>(OnUploadHeader);
        SubscribeLocalEvent<MusicRecorderComponent, MusicRecorderUploadChunkMessage>(OnUploadChunk);
        SubscribeLocalEvent<MusicRecorderComponent, MusicRecorderUploadFinishMessage>(OnUploadFinish);
        SubscribeLocalEvent<MusicRecorderComponent, MusicRecorderRemoveTrackMessage>(OnRemoveTrack);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _uploads.Clear();

        var audioQuery = EntityQueryEnumerator<MusicCassetteAudioComponent>();
        while (audioQuery.MoveNext(out _, out var audio))
        {
            audio.Audio.Clear();
        }

        var cassetteQuery = EntityQueryEnumerator<MusicCassetteComponent>();
        while (cassetteQuery.MoveNext(out var uid, out var cassette))
        {
            if (cassette.Tracks.Count == 0)
                continue;

            cassette.Tracks.Clear();
            Dirty(uid, cassette);
        }
    }

    private void OnCassetteInserted(Entity<MusicRecorderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != MusicRecorderComponent.CassetteSlotId)
            return;

        UpdateUi(ent);
    }

    private void OnCassetteRemoved(Entity<MusicRecorderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != MusicRecorderComponent.CassetteSlotId)
            return;

        UpdateUi(ent);
    }

    private void OnUiOpened(Entity<MusicRecorderComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    private void OnUiClosed(Entity<MusicRecorderComponent> ent, ref BoundUIClosedEvent args)
    {
        _uploads.Remove((ent.Owner, args.Actor));
    }

    private void OnRename(Entity<MusicRecorderComponent> ent, ref MusicRecorderRenameMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!TryGetCassette(ent, out var cassette) || !TryComp<MusicCassetteComponent>(cassette, out _))
            return;

        var name = args.Name.Trim();
        if (!IsValidName(name))
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-invalid-name"), actor, actor);
            return;
        }

        _metaData.SetEntityName(cassette, name);
        _popup.PopupEntity(Loc.GetString("music-recorder-renamed", ("name", name)), actor, actor);
        UpdateUi(ent);
    }

    private void OnUploadHeader(Entity<MusicRecorderComponent> ent, ref MusicRecorderUploadHeaderMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        _uploads.Remove((ent.Owner, actor));

        if (!TryGetCassette(ent, out var cassette) || !TryComp<MusicCassetteComponent>(cassette, out var cassetteComp))
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-no-cassette-upload"), actor, actor);
            return;
        }

        if (cassetteComp.Tracks.Count >= MusicCassetteComponent.MaxTracks)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-too-many"), actor, actor);
            return;
        }

        if (args.TotalBytes <= 0 || args.TotalBytes > MusicCassetteComponent.MaxTrackBytes)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-too-large"), actor, actor);
            return;
        }

        if (args.Duration < 0.5f || args.Duration > MusicCassetteComponent.MaxDurationSeconds)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-too-long"), actor, actor);
            return;
        }

        _uploads[(ent.Owner, actor)] = new PendingUpload
        {
            TransferId = args.TransferId,
            Name = SanitizeTrackName(args.Name),
            Duration = args.Duration,
            Buffer = new byte[args.TotalBytes],
            Started = _timing.CurTime,
        };
    }

    private void OnUploadChunk(Entity<MusicRecorderComponent> ent, ref MusicRecorderUploadChunkMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!_uploads.TryGetValue((ent.Owner, actor), out var upload) || upload.TransferId != args.TransferId)
            return;

        if (args.Data.Length == 0 ||
            args.Data.Length > MusicCassetteComponent.ChunkBytes ||
            args.Offset != upload.Received ||
            args.Offset + args.Data.Length > upload.Buffer.Length)
        {
            _uploads.Remove((ent.Owner, actor));
            return;
        }

        args.Data.CopyTo(upload.Buffer, args.Offset);
        upload.Received += args.Data.Length;
    }

    private void OnUploadFinish(Entity<MusicRecorderComponent> ent, ref MusicRecorderUploadFinishMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!_uploads.Remove((ent.Owner, actor), out var upload) || upload.TransferId != args.TransferId)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-upload-failed"), actor, actor);
            return;
        }

        if (upload.Received != upload.Buffer.Length)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-upload-failed"), actor, actor);
            return;
        }

        if (!MusicOgg.TryGetChannelCount(upload.Buffer, out var channels))
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-not-ogg"), actor, actor);
            return;
        }

        if (channels != 1)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-not-mono"), actor, actor);
            return;
        }

        if (!TryGetCassette(ent, out var cassette) || !TryComp<MusicCassetteComponent>(cassette, out var cassetteComp))
            return;

        if (cassetteComp.Tracks.Count >= MusicCassetteComponent.MaxTracks)
        {
            _popup.PopupEntity(Loc.GetString("music-recorder-too-many"), actor, actor);
            return;
        }

        var id = Guid.NewGuid().ToString("N");
        cassetteComp.Tracks.Add(new MusicCassetteTrack
        {
            Id = id,
            Name = upload.Name,
            Duration = upload.Duration,
            Size = upload.Buffer.Length,
        });
        Dirty(cassette, cassetteComp);

        var audio = EnsureComp<MusicCassetteAudioComponent>(cassette);
        audio.Audio[id] = upload.Buffer;

        _popup.PopupEntity(Loc.GetString("music-recorder-track-added", ("name", upload.Name)), actor, actor);
        UpdateUi(ent);
    }

    private void OnRemoveTrack(Entity<MusicRecorderComponent> ent, ref MusicRecorderRemoveTrackMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!TryGetCassette(ent, out var cassette) || !TryComp<MusicCassetteComponent>(cassette, out var cassetteComp))
            return;

        var trackId = args.TrackId;
        var index = cassetteComp.Tracks.FindIndex(track => track.Id == trackId);
        if (index < 0)
            return;

        var name = cassetteComp.Tracks[index].Name;
        cassetteComp.Tracks.RemoveAt(index);
        Dirty(cassette, cassetteComp);

        if (TryComp<MusicCassetteAudioComponent>(cassette, out var audio))
            audio.Audio.Remove(args.TrackId);

        _popup.PopupEntity(Loc.GetString("music-recorder-track-removed", ("name", name)), actor, actor);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<MusicRecorderComponent> ent)
    {
        string? name = null;
        var tracks = Array.Empty<MusicCassetteTrack>();
        var hasCassette = TryGetCassette(ent, out var cassette);

        if (hasCassette && TryComp<MusicCassetteComponent>(cassette, out var cassetteComp))
        {
            name = Name(cassette);
            tracks = cassetteComp.Tracks.ToArray();
        }
        else
        {
            hasCassette = false;
        }

        _ui.SetUiState(ent.Owner, MusicRecorderUiKey.Key, new MusicRecorderBoundUserInterfaceState(hasCassette, name, tracks));
    }

    private static bool IsValidName(string name)
    {
        if (name.Length == 0 || name.Length > MusicRecorderComponent.MaxCassetteNameLength)
            return false;

        foreach (var character in name)
        {
            if (char.IsControl(character) || character is '[' or ']')
                return false;
        }

        return true;
    }

    private static string SanitizeTrackName(string name)
    {
        name = name.Trim();
        if (name.Length > MusicRecorderComponent.MaxCassetteNameLength)
            name = name[..MusicRecorderComponent.MaxCassetteNameLength];

        var buffer = new char[name.Length];
        var count = 0;
        foreach (var character in name)
        {
            if (char.IsControl(character) || character is '[' or ']')
                continue;

            buffer[count++] = character;
        }

        var sanitized = new string(buffer, 0, count).Trim();
        return sanitized.Length == 0 ? "Track" : sanitized;
    }

    private sealed class PendingUpload
    {
        public int TransferId;
        public string Name = string.Empty;
        public float Duration;
        public byte[] Buffer = Array.Empty<byte>();
        public int Received;
        public TimeSpan Started;
    }
}
