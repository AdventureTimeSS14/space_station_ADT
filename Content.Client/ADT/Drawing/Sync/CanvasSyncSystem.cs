using System.Linq;
using System.Threading.Tasks;
using Content.Client.ADT.Drawing.Cache;
using Content.Client.ADT.Drawing.Canvas;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;
using Robust.Client.Player;
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
    private static readonly TimeSpan LayerMetaInterval = TimeSpan.FromMilliseconds(150);

    [Dependency] private readonly CanvasNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CanvasTileCacheSystem _cache = default!;
    [Dependency] private readonly CanvasBlobSystem _blobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CanvasLayerStoreSystem _layerStore = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly BrushSettings _brush = new();
    private CanvasSyncSession? _active;
    private TimeSpan _debounce;
    private double _upRate;
    private (Guid Painting, List<CanvasParticipantInfo> List, NetUserId? Local)? _participants;
    private readonly Dictionary<NetUserId, string> _names = new();

    public CanvasSyncSession? Active => _active;

    public event Action<CanvasSyncSession>? SessionStarted;
    public event Action<CanvasSyncSession>? SessionUpdated;
    public event Action<CanvasSyncSession, CanvasCloseReason>? SessionClosed;
    public event Action<CanvasSyncSession, string>? LayerSyncFailed;
    public event Action<CanvasSyncSession>? FlushReady;

    public override void Initialize()
    {
        base.Initialize();

        _net.ManifestReceived += OnManifest;
        _net.SessionClosed += OnSessionClosed;
        _net.CommitResult += OnCommitResult;
        _net.TileUpdated += OnTileUpdated;
        _net.StrokeReceived += OnStroke;
        _net.CursorReceived += OnCursor;
        _net.LayersReceived += OnLayers;
        _net.FlushReceived += OnFlush;
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
        _net.LayersReceived -= OnLayers;
        _net.FlushReceived -= OnFlush;
        _blobs.BlobArrived -= OnBlobArrived;

        if (_active != null)
            Drop(_active, CanvasCloseReason.Closed);
    }

    public void SetParticipants(Guid painting, List<CanvasParticipantInfo> participants, NetUserId? localUser)
    {
        _participants = (painting, participants, localUser);
        foreach (var participant in participants)
        {
            _names[participant.User] = participant.Name;
        }

        if (_active is not { } session || session.Painting != painting)
            return;

        session.Collaboration.SetParticipants(participants, localUser);
        var layers = session.Document.Layers;
        layers.IsHost = session.Collaboration.LocalRole == CanvasRole.Host;
        layers.RefreshOwnerNames(OwnerName);
        RegisterLayers(session);
    }

    private string OwnerName(NetUserId user)
    {
        return _names.TryGetValue(user, out var name) ? name : "?";
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
        UpdateFlush(session);

        if (now >= session.NextLayerSave)
        {
            session.NextLayerSave = now + LayerSaveInterval;
            if (session.Document.Layers.AnyDirty())
                _layerStore.Save(session.Painting, session.Document.Layers);
        }

        if (now >= session.NextLayerMeta)
        {
            session.NextLayerMeta = now + LayerMetaInterval;
            SendPendingOpacity(session);
        }

        if (!session.Loaded || session.Error != null)
            return;

        PumpUploads(session, now);

        var busy = session.Commits.Count + session.Encoding.Count;
        if (now < session.RetryAfter || busy >= MaxCommitsInFlight || document.Stroker.Active)
            return;

        var idle = now - session.LastEdit >= _debounce;
        var overdue = session.DirtySince != TimeSpan.Zero && now - session.DirtySince >= MaxCommitDelay;
        if (!idle && !overdue && !session.Detached && !session.Flushing)
            return;

        foreach (var index in document.ModifiedTiles().ToList())
        {
            if (session.Commits.Count + session.Encoding.Count >= MaxCommitsInFlight || session.Encoding.Count >= MaxEncodes)
                break;

            StartCommit(session, index);
        }

        foreach (var (layer, index) in document.ModifiedLayerTiles().ToList())
        {
            if (session.Commits.Count + session.Encoding.Count >= MaxCommitsInFlight || session.Encoding.Count >= MaxEncodes)
                break;

            StartLayerCommit(session, layer, index);
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
            document.Layers.ActionRequested += (kind, layer) => OnLayerAction(session, kind, layer);

            if (_participants is { } participants && participants.Painting == msg.Painting)
            {
                collaboration.SetParticipants(participants.List, participants.Local);
                document.Layers.IsHost = collaboration.LocalRole == CanvasRole.Host;
            }
        }

        session.Document.Layers.ApplyServer(msg.Layers, _player.LocalSession?.UserId, true, OwnerName);

        session.Waiting.Clear();
        session.DecodeQueue.Clear();
        session.LoadedBlobs = 0;
        session.ForceRequest = true;

        foreach (var tile in msg.Tiles)
        {
            ApplyRemote(session, tile.Key, tile.Version, tile.Content);
        }

        session.TotalBlobs = session.DecodeQueue.Count;
        RegisterLayers(session);
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
        if (key.Layer != CanvasConstants.BaseLayer)
        {
            ApplyRemoteLayer(session, key, version, content, author, strokeIds);
            return;
        }

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

    private void ApplyRemoteLayer(CanvasSyncSession session, CanvasTileKey key, uint version, CanvasTileContent content,
        byte author, uint[]? strokeIds)
    {
        var document = session.Document;
        if (document.Layers.FindServer(key.Layer) is not { } layer || !document.TryGetTileIndex(key, out var index))
            return;

        if (document.IsLayerStale(layer, index, version) ||
            content.Kind != CanvasTileKind.Blob ||
            layer.Own && !layer.UseServerCopy)
        {
            document.ApplyLayerTile(layer, index, version, content, null);
            ConfirmPreview(session, author, strokeIds, index);
            return;
        }

        session.DecodeQueue.Enqueue(new CanvasDecodeJob(index, version, content, null, layer, author, strokeIds));
    }

    private static void ConfirmPreview(CanvasSyncSession session, byte author, uint[]? strokeIds, int index)
    {
        if (author != CanvasConstants.NoAuthor && strokeIds is { Length: > 0 })
            session.Collaboration.TileConfirmed(author, strokeIds, index);
    }

    private bool IsStale(CanvasSyncSession session, CanvasDecodeJob job)
    {
        var document = session.Document;
        if (job.Layer == null)
            return document.IsStale(job.Index, job.Version);

        return !document.Layers.Layers.Contains(job.Layer) || document.IsLayerStale(job.Layer, job.Index, job.Version);
    }

    private void Wait(CanvasSyncSession session, CanvasDecodeJob job)
    {
        if (!session.Waiting.TryGetValue(job.Content.Hash, out var waiting))
        {
            waiting = new List<CanvasWaitingTile>();
            session.Waiting[job.Content.Hash] = waiting;
        }

        waiting.Add(new CanvasWaitingTile(job.Index, job.Version, job.Content, job.Layer, job.Author, job.StrokeIds));
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
                if (run.Job.Layer is { } layer)
                {
                    if (document.Layers.Layers.Contains(layer))
                        document.ApplyLayerTile(layer, run.Job.Index, run.Job.Version, run.Job.Content, pixels);

                    ConfirmPreview(session, run.Job.Author, run.Job.StrokeIds, run.Job.Index);
                    continue;
                }

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
            if (IsStale(session, job))
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
            session.DecodeQueue.Enqueue(new CanvasDecodeJob(tile.Index, tile.Version, tile.Content, data,
                tile.Layer, tile.Author, tile.StrokeIds));
        }
    }

    public void RequestFlush(Guid painting)
    {
        if (_active is not { } session || session.Painting != painting)
            return;

        session.FlushRequested = true;
        session.FlushReadyPending = false;
        _net.Send(new MsgCanvasFlush
        {
            Painting = painting,
            Phase = CanvasFlushPhase.Request,
        });
    }

    public void CancelFlush(Guid painting)
    {
        if (_active is not { } session || session.Painting != painting)
            return;

        session.FlushRequested = false;
        session.FlushReadyPending = false;
    }

    private void OnFlush(MsgCanvasFlush msg)
    {
        if (msg.Malformed || _active is not { } session || session.Painting != msg.Painting)
            return;

        switch (msg.Phase)
        {
            case CanvasFlushPhase.Flush:
                session.FlushId = msg.Id;
                session.Flushing = true;
                break;

            case CanvasFlushPhase.Ready:
                session.FlushId = 0;
                session.Flushing = false;
                if (session.FlushRequested)
                {
                    session.FlushRequested = false;
                    session.FlushReadyPending = true;
                }
                break;

            default:
                return;
        }

        SessionUpdated?.Invoke(session);
    }

    private void UpdateFlush(CanvasSyncSession session)
    {
        if (session.FlushId != 0 &&
            !session.Document.ActiveStroke &&
            !session.Document.HasPendingWork)
        {
            _net.Send(new MsgCanvasFlush
            {
                Painting = session.Painting,
                Phase = CanvasFlushPhase.Done,
                Id = session.FlushId,
            });
            session.FlushId = 0;
        }

        if (session.FlushReadyPending && session.Loaded && session.Error == null)
        {
            session.FlushReadyPending = false;
            FlushReady?.Invoke(session);
        }
    }

    private void OnLayers(MsgCanvasLayers msg)
    {
        if (msg.Malformed || _active is not { } session || session.Painting != msg.Painting)
            return;

        session.Document.Layers.ApplyServer(msg.Layers, _player.LocalSession?.UserId, false, OwnerName);
        RegisterLayers(session);
        SessionUpdated?.Invoke(session);
    }

    private bool CanCreateLayers(CanvasSyncSession session)
    {
        return session.Collaboration.LocalRole != CanvasRole.Viewer;
    }

    private void RegisterLayers(CanvasSyncSession session)
    {
        if (!CanCreateLayers(session))
            return;

        foreach (var layer in session.Document.Layers.Layers.ToList())
        {
            if (layer.Own && !layer.Registered && !layer.CreateSent)
                OnLayerAction(session, CanvasLayerActionKind.Create, layer);
        }
    }

    private void OnLayerAction(CanvasSyncSession session, CanvasLayerActionKind kind, CanvasLayer layer)
    {
        if (_active != session)
            return;

        var msg = new MsgCanvasLayerAction
        {
            Painting = session.Painting,
            Action = kind,
            Layer = layer.ServerId,
            LocalId = layer.Own ? layer.Id : layer.OwnerLocalId,
            Name = layer.Name,
            Opacity = layer.Opacity,
            Visible = layer.Visible,
            Delta = layer.LastMove,
        };

        if (kind == CanvasLayerActionKind.Create)
        {
            if (!layer.Own || layer.Registered || layer.CreateSent || !CanCreateLayers(session))
                return;

            layer.CreateSent = true;
            _net.Send(msg);
            return;
        }

        if (!layer.Registered)
            return;

        _net.Send(msg);
    }

    private void SendPendingOpacity(CanvasSyncSession session)
    {
        foreach (var layer in session.Document.Layers.Layers)
        {
            if (!layer.OpacityPending || !layer.Registered)
                continue;

            layer.OpacityPending = false;
            OnLayerAction(session, CanvasLayerActionKind.Opacity, layer);
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

    private void StartLayerCommit(CanvasSyncSession session, CanvasLayer layer, int index)
    {
        var prepared = session.Document.PrepareLayerCommit(layer, index);
        var run = new CanvasEncodeRun(index, layer);
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
                if (run.Layer is { } layer)
                    session.Document.RejectLayerCommit(layer, run.Index, null);
                else
                    session.Document.RejectCommit(run.Index);

                session.RetryAfter = _timing.RealTime + RetryDelay;
                continue;
            }

            if (run.Layer is { } target && (!session.Document.Layers.Layers.Contains(target) || !target.Registered))
                continue;

            SendCommit(session, run.Index, commit, run.Layer);
        }
    }

    private void SendCommit(CanvasSyncSession session, int index, CanvasTileCommit commit, CanvasLayer? layer = null)
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

        session.Commits[msg.CommitId] = new CanvasClientCommit(index, commit, transferId, layer);
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

        if (commit.Layer is { } layer)
        {
            OnLayerCommitResult(session, commit, layer, msg);
            return;
        }

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

    private void OnLayerCommitResult(CanvasSyncSession session, CanvasClientCommit commit, CanvasLayer layer,
        MsgCanvasCommitResult msg)
    {
        var document = session.Document;
        if (!document.Layers.Layers.Contains(layer))
            return;

        switch (msg.Result)
        {
            case CanvasCommitResult.Ok:
                document.ConfirmLayerCommit(layer, commit.Index, msg.Version);
                if (commit.Commit.Png != null && msg.Content.Kind == CanvasTileKind.Blob)
                    _cache.Put(msg.Content.Hash, commit.Commit.Png);
                break;

            case CanvasCommitResult.VersionMismatch:
                document.RejectLayerCommit(layer, commit.Index, msg.Version);
                break;

            case CanvasCommitResult.Busy:
                document.RejectLayerCommit(layer, commit.Index, null);
                session.RetryAfter = _timing.RealTime + RetryDelay;
                break;

            default:
                document.RejectLayerCommit(layer, commit.Index, null);
                layer.SyncPending = false;
                var error = msg.Result == CanvasCommitResult.TooLarge
                    ? "adt-canvas-layer-sync-too-large"
                    : "adt-canvas-layer-sync-failed";
                LayerSyncFailed?.Invoke(session, Loc.GetString(error, ("name", layer.Name)));
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
