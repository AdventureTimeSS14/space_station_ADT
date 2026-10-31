using System.Linq;
using System.Threading.Tasks;
using Content.Client.ADT.Drawing.Cache;
using Content.Client.ADT.Drawing.Canvas;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Sync;

public sealed class CanvasSyncSystem : EntitySystem
{
    private const int MaxCommitsInFlight = 4;
    private const int MaxRebases = 5;
    private const int MaxEncodes = 2;
    private const int MaxDecodes = 2;

    private static readonly TimeSpan MaxCommitDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DetachTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan LayerSaveInterval = TimeSpan.FromSeconds(30);

    [Dependency] private readonly CanvasNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CanvasTileCacheSystem _cache = default!;
    [Dependency] private readonly CanvasBlobSystem _blobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CanvasLayerStoreSystem _layerStore = default!;

    private readonly BrushSettings _brush = new();
    private CanvasSyncSession? _active;
    private TimeSpan _debounce;
    private double _upRate;
    private (Guid Painting, List<CanvasParticipantInfo> List, NetUserId? Local)? _participants;

    public CanvasSyncSession? Active => _active;

    public event Action<CanvasSyncSession>? SessionStarted;
    public event Action<CanvasSyncSession>? SessionUpdated;
    public event Action<CanvasSyncSession, CanvasCloseReason>? SessionClosed;

    public override void Initialize()
    {
        base.Initialize();

        _net.ManifestReceived += OnManifest;
        _net.SessionClosed += OnSessionClosed;
        _net.CommitResult += OnCommitResult;
        _net.TileUpdated += OnTileUpdated;
        _net.StrokeReceived += OnStroke;
        _net.CursorReceived += OnCursor;
        _blobs.BlobArrived += OnBlobArrived;

        Subs.CVar(_cfg, ADTCCVars.CanvasCommitDebounceMs, v => _debounce = TimeSpan.FromMilliseconds(Math.Max(0, v)), true);
        Subs.CVar(_cfg, ADTCCVars.CanvasUpKbps, v => _upRate = Math.Max(1, v) * 1024.0, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _net.ManifestReceived -= OnManifest;
        _net.SessionClosed -= OnSessionClosed;
        _net.CommitResult -= OnCommitResult;
        _net.TileUpdated -= OnTileUpdated;
        _net.StrokeReceived -= OnStroke;
        _net.CursorReceived -= OnCursor;
        _blobs.BlobArrived -= OnBlobArrived;

        if (_active != null)
            Drop(_active, CanvasCloseReason.Closed);
    }

    public void SetParticipants(Guid painting, List<CanvasParticipantInfo> participants, NetUserId? localUser)
    {
        _participants = (painting, participants, localUser);
        if (_active is { } session && session.Painting == painting)
            session.Collaboration.SetParticipants(participants, localUser);
    }

    public void Detach(Guid painting)
    {
        if (_active == null || _active.Painting != painting)
            return;

        _active.Detached = true;
        _active.DetachDeadline = _timing.RealTime + DetachTimeout;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_active is not { } session)
            return;

        var now = _timing.RealTime;
        var document = session.Document;

        if (session.Detached && (!document.HasPendingWork || session.Error != null || now >= session.DetachDeadline))
        {
            Drop(session, CanvasCloseReason.Closed);
            return;
        }

        session.Collaboration.Update();
        PumpDecodes(session);
        PumpEncodes(session);

        if (now >= session.NextLayerSave)
        {
            session.NextLayerSave = now + LayerSaveInterval;
            if (session.Document.Layers.AnyDirty())
                _layerStore.Save(session.Painting, session.Document.Layers);
        }

        if (!session.Loaded || session.Error != null)
            return;

        PumpUploads(session, now);

        var busy = session.Commits.Count + session.Encoding.Count;
        if (now < session.RetryAfter || busy >= MaxCommitsInFlight || document.Stroker.Active)
            return;

        var idle = now - session.LastEdit >= _debounce;
        var overdue = session.DirtySince != TimeSpan.Zero && now - session.DirtySince >= MaxCommitDelay;
        if (!idle && !overdue && !session.Detached)
            return;

        foreach (var index in document.ModifiedTiles().ToList())
        {
            if (session.Commits.Count + session.Encoding.Count >= MaxCommitsInFlight || session.Encoding.Count >= MaxEncodes)
                break;

            StartCommit(session, index);
        }

        if (!document.ModifiedTiles().Any())
            session.DirtySince = TimeSpan.Zero;
    }

    private void SetPixelMode(bool pixel)
    {
        if (_brush.Pixel == pixel)
            return;

        _brush.Pixel = pixel;
        _brush.Size = pixel ? 1f : new BrushSettings().Size;
    }

    #region Манифест и тайлы

    private void OnManifest(MsgCanvasManifest msg)
    {
        if (msg.Malformed || !CanvasConstants.IsValidSize(new Vector2i(msg.Width, msg.Height)))
            return;

        CanvasSyncSession session;
        if (_active != null &&
            _active.Painting == msg.Painting &&
            _active.Document.Width == msg.Width &&
            _active.Document.Height == msg.Height)
        {
            session = _active;
            session.Detached = false;
            session.Error = null;
        }
        else
        {
            if (_active != null)
                Drop(_active, CanvasCloseReason.Closed);

            SetPixelMode(CanvasConstants.IsPixelSize(new Vector2i(msg.Width, msg.Height)));
            var document = new CanvasDocument(msg.Width, msg.Height, Color.White, _brush);
            var collaboration = new CanvasCollaboration(msg.Painting, document, _net.Send, _timing);

            session = new CanvasSyncSession(msg.Painting, document, collaboration)
            {
                LastRefill = _timing.RealTime,
                UpTokens = UpCapacity(),
            };

            document.Edited += () => OnEdited(session);
            document.TileApplied += index => OnTileApplied(session, index);
            _active = session;
            session.NextLayerSave = _timing.RealTime + LayerSaveInterval;
            _layerStore.Load(msg.Painting, document.Layers, document.Surface.TileCount, document.Surface.GetTileSize);

            if (_participants is { } participants && participants.Painting == msg.Painting)
                collaboration.SetParticipants(participants.List, participants.Local);
        }

        session.Waiting.Clear();
        session.DecodeQueue.Clear();
        session.LoadedBlobs = 0;
        session.ForceRequest = true;

        foreach (var tile in msg.Tiles)
        {
            ApplyRemote(session, tile.Key, tile.Version, tile.Content);
        }

        session.TotalBlobs = session.DecodeQueue.Count;
        SessionStarted?.Invoke(session);
    }

    private void OnEdited(CanvasSyncSession session)
    {
        var now = _timing.RealTime;
        session.LastEdit = now;
        if (session.DirtySince == TimeSpan.Zero)
            session.DirtySince = now;
    }

    private void OnTileApplied(CanvasSyncSession session, int index)
    {
        if (!session.Confirmations.TryGetValue(index, out var confirmations))
            return;

        var version = session.Document.GetTileState(index).Version;
        confirmations.RemoveAll(c =>
        {
            if (c.Version > version)
                return false;

            session.Collaboration.TileConfirmed(c.Author, c.StrokeIds, index);
            return true;
        });

        if (confirmations.Count == 0)
            session.Confirmations.Remove(index);
    }

    private void ApplyRemote(CanvasSyncSession session, CanvasTileKey key, uint version, CanvasTileContent content,
        byte author = CanvasConstants.NoAuthor, uint[]? strokeIds = null)
    {
        var document = session.Document;
        if (!document.TryGetIndex(key, out var index))
            return;

        var state = document.GetTileState(index);
        if (version < state.Version || version == state.Version && state.Version != 0)
        {
            if (author != CanvasConstants.NoAuthor && strokeIds != null)
                session.Collaboration.TileConfirmed(author, strokeIds, index);

            return;
        }

        if (author != CanvasConstants.NoAuthor && strokeIds is { Length: > 0 })
        {
            if (!session.Confirmations.TryGetValue(index, out var confirmations))
            {
                confirmations = new List<CanvasConfirmation>();
                session.Confirmations[index] = confirmations;
            }

            confirmations.Add(new CanvasConfirmation(version, author, strokeIds));
        }

        if (content.Kind != CanvasTileKind.Blob)
        {
            document.ApplyAuthoritative(index, version, content, null);
            return;
        }

        session.DecodeQueue.Enqueue(new CanvasDecodeJob(index, version, content, null));
    }

    private void Wait(CanvasSyncSession session, CanvasDecodeJob job)
    {
        if (!session.Waiting.TryGetValue(job.Content.Hash, out var waiting))
        {
            waiting = new List<CanvasWaitingTile>();
            session.Waiting[job.Content.Hash] = waiting;
        }

        waiting.Add(new CanvasWaitingTile(job.Index, job.Version, job.Content));
    }

    private void PumpDecodes(CanvasSyncSession session)
    {
        var document = session.Document;
        var changed = false;

        for (var i = session.Decoding.Count - 1; i >= 0; i--)
        {
            var run = session.Decoding[i];
            if (!run.Task.IsCompleted)
                continue;

            session.Decoding.RemoveAt(i);
            changed = true;
            session.LoadedBlobs++;

            var pixels = run.Task.IsCompletedSuccessfully ? run.Result : null;
            if (pixels != null)
            {
                document.ApplyAuthoritative(run.Job.Index, run.Job.Version, run.Job.Content, pixels);
                continue;
            }

            if (run.FromCache)
            {
                _cache.Invalidate(new[] { run.Job.Content.Hash });
                Wait(session, run.Job);
                continue;
            }

            Log.Error($"Canvas tile {run.Job.Index} of {session.Painting} failed to decode");
            session.Error = "adt-canvas-sync-error-corrupt";
        }

        var waited = false;
        while (session.Decoding.Count < MaxDecodes && session.DecodeQueue.TryDequeue(out var job))
        {
            if (document.IsStale(job.Index, job.Version))
            {
                session.LoadedBlobs++;
                changed = true;
                continue;
            }

            var png = job.Png;
            var fromCache = false;
            if (png == null)
            {
                if (!_cache.TryGet(job.Content.Hash, out var cached))
                {
                    Wait(session, job);
                    waited = true;
                    continue;
                }

                png = cached;
                fromCache = true;
            }

            var size = document.Surface.GetTileSize(job.Index);
            var data = png;
            var run = new CanvasDecodeRun(job, fromCache);
            run.Task = Task.Run(() =>
            {
                run.Result = Decode(data, size);
            });
            session.Decoding.Add(run);
        }

        if (waited)
            RequestMissing(session);

        if (changed)
            SessionUpdated?.Invoke(session);
    }

    private static Rgba32[]? Decode(byte[] png, Vector2i size)
    {
        var pixels = new Rgba32[size.X * size.Y];
        return CanvasTileCodec.TryDecodePng(png, size.X, size.Y, pixels) ? pixels : null;
    }

    private void RequestMissing(CanvasSyncSession session)
    {
        if (session.Waiting.Count == 0)
            return;

        _blobs.Request(session.Painting, session.Waiting.Keys, session.ForceRequest);
        session.ForceRequest = false;
    }

    private void OnTileUpdated(MsgCanvasTileUpdated msg)
    {
        if (_active is not { } session || session.Painting != msg.Painting)
            return;

        ApplyRemote(session, msg.Key, msg.Version, msg.Content, msg.Author, msg.StrokeIds);
    }

    private void OnStroke(MsgCanvasStroke msg)
    {
        if (msg.Malformed || _active is not { } session || session.Painting != msg.Painting)
            return;

        session.Collaboration.HandleStroke(msg);
    }

    private void OnCursor(MsgCanvasCursor msg)
    {
        if (msg.Malformed || _active is not { } session || session.Painting != msg.Painting)
            return;

        session.Collaboration.HandleCursor(msg);
    }

    private void OnBlobArrived(CanvasHash hash, byte[] data)
    {
        if (_active is not { } session || !session.Waiting.Remove(hash, out var tiles))
            return;

        foreach (var tile in tiles)
        {
            session.DecodeQueue.Enqueue(new CanvasDecodeJob(tile.Index, tile.Version, tile.Content, data));
        }
    }

    #endregion

    #region Коммиты

    private void StartCommit(CanvasSyncSession session, int index)
    {
        var prepared = session.Document.PrepareCommit(index);
        var run = new CanvasEncodeRun(index);
        run.Task = Task.Run(() =>
        {
            run.Result = CanvasDocument.Encode(prepared);
        });
        session.Encoding.Add(run);
    }

    private void PumpEncodes(CanvasSyncSession session)
    {
        for (var i = session.Encoding.Count - 1; i >= 0; i--)
        {
            var run = session.Encoding[i];
            if (!run.Task.IsCompleted)
                continue;

            session.Encoding.RemoveAt(i);
            if (!run.Task.IsCompletedSuccessfully || run.Result is not { } commit)
            {
                Log.Error($"Failed to encode canvas tile {run.Index} of {session.Painting}");
                session.Document.RejectCommit(run.Index);
                session.RetryAfter = _timing.RealTime + RetryDelay;
                continue;
            }

            SendCommit(session, run.Index, commit);
        }
    }

    private void SendCommit(CanvasSyncSession session, int index, CanvasTileCommit commit)
    {
        var msg = new MsgCanvasTileCommit
        {
            Painting = session.Painting,
            CommitId = session.NextCommitId++,
            Key = commit.Key,
            BaseVersion = commit.BaseVersion,
            Content = commit.Content,
            StrokeIds = commit.Operations,
        };

        uint transferId = 0;
        if (commit.Png != null)
        {
            transferId = session.NextTransferId++;
            msg.TransferId = transferId;
            msg.Size = commit.Png.Length;
            session.Uploads.Enqueue(new CanvasClientUpload(transferId, commit.Png));
        }

        session.Commits[msg.CommitId] = new CanvasClientCommit(index, commit, transferId);
        _net.Send(msg);
    }

    private void OnCommitResult(MsgCanvasCommitResult msg)
    {
        if (_active is not { } session ||
            session.Painting != msg.Painting ||
            !session.Commits.Remove(msg.CommitId, out var commit))
        {
            return;
        }

        var document = session.Document;
        if (msg.Result != CanvasCommitResult.Ok)
            CancelUpload(session, commit.TransferId);

        switch (msg.Result)
        {
            case CanvasCommitResult.Ok:
                session.Rebases.Remove(commit.Index);
                document.ConfirmCommit(commit.Index, msg.Version, msg.Content);
                if (commit.Commit.Png != null && msg.Content.Kind == CanvasTileKind.Blob)
                    _cache.Put(msg.Content.Hash, commit.Commit.Png);
                break;

            case CanvasCommitResult.VersionMismatch:
                document.RejectCommit(commit.Index);
                ApplyRemote(session, msg.Key, msg.Version, msg.Content);
                RequestMissing(session);

                session.Rebases.TryGetValue(commit.Index, out var rebases);
                rebases++;
                if (rebases > MaxRebases)
                {
                    session.Rebases.Remove(commit.Index);
                    session.RetryAfter = _timing.RealTime + TimeSpan.FromSeconds(_random.NextFloat(1f, 2f));
                }
                else
                {
                    session.Rebases[commit.Index] = rebases;
                }
                break;

            case CanvasCommitResult.Busy:
                document.RejectCommit(commit.Index);
                session.RetryAfter = _timing.RealTime + RetryDelay;
                break;

            case CanvasCommitResult.TooLarge:
                document.RejectCommit(commit.Index);
                session.Error = "adt-canvas-sync-error-too-large";
                SessionUpdated?.Invoke(session);
                break;

            case CanvasCommitResult.NotAllowed:
                document.RejectCommit(commit.Index);
                session.Error = "adt-canvas-sync-error-not-allowed";
                SessionUpdated?.Invoke(session);
                break;

            default:
                document.RejectCommit(commit.Index);
                session.Error = "adt-canvas-sync-error-invalid";
                SessionUpdated?.Invoke(session);
                break;
        }
    }

    private void CancelUpload(CanvasSyncSession session, uint transferId)
    {
        if (transferId == 0)
            return;

        var kept = session.Uploads.Where(u => u.TransferId != transferId).ToList();
        session.Uploads.Clear();
        foreach (var upload in kept)
        {
            session.Uploads.Enqueue(upload);
        }
    }

    private double UpCapacity()
    {
        return Math.Max(CanvasConstants.ChunkSize * 2.0, _upRate / 2);
    }

    private void PumpUploads(CanvasSyncSession session, TimeSpan now)
    {
        if (session.Uploads.Count == 0)
        {
            session.LastRefill = now;
            return;
        }

        var dt = (now - session.LastRefill).TotalSeconds;
        session.LastRefill = now;
        if (dt > 0)
            session.UpTokens = Math.Min(UpCapacity(), session.UpTokens + _upRate * dt);

        while (session.Uploads.TryPeek(out var upload))
        {
            var offset = upload.NextIndex * CanvasConstants.ChunkSize;
            var length = Math.Min(CanvasConstants.ChunkSize, upload.Data.Length - offset);
            if (session.UpTokens < length)
                return;

            var data = new byte[length];
            Array.Copy(upload.Data, offset, data, 0, length);

            _net.Send(new MsgCanvasChunk
            {
                TransferId = upload.TransferId,
                TotalLength = upload.Data.Length,
                Index = upload.NextIndex,
                Count = upload.Count,
                Data = data,
            });

            session.UpTokens -= length;
            upload.NextIndex++;
            if (upload.NextIndex >= upload.Count)
                session.Uploads.Dequeue();
        }
    }

    #endregion

    private void OnSessionClosed(MsgCanvasSessionClosed msg)
    {
        if (_active is { } session && session.Painting == msg.Painting)
            Drop(session, msg.Reason);
    }

    private void Drop(CanvasSyncSession session, CanvasCloseReason reason)
    {
        if (_active == session)
            _active = null;

        SessionClosed?.Invoke(session, reason);

        if (reason == CanvasCloseReason.Submitted)
            _layerStore.Delete(session.Painting);
        else if (session.Document.Layers.AnyDirty())
            _layerStore.Save(session.Painting, session.Document.Layers);

        session.Collaboration.Dispose();
        session.Document.Dispose();
    }
}
