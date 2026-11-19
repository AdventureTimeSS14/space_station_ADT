using System.Threading.Tasks;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem
{
    private const int MaxIncomingPerPeer = 8;
    private const int MaxPendingPerPeer = 8;
    private const int MaxValidating = 32;
    private const int MaxTransfersInFlight = 4;
    private const long MaxBytesInFlight = 256 * 1024;

    private static readonly TimeSpan TransferTimeout = TimeSpan.FromSeconds(30);

    private readonly Dictionary<NetUserId, CanvasPeer> _peers = new();
    private int _validating;
    private double _downRate;
    private double _upRate;

    private void InitializeNet()
    {
        _net.RegisterNetMessage<MsgCanvasManifest>();
        _net.RegisterNetMessage<MsgCanvasSessionClosed>();
        _net.RegisterNetMessage<MsgCanvasTileCommit>(OnTileCommit);
        _net.RegisterNetMessage<MsgCanvasCommitResult>();
        _net.RegisterNetMessage<MsgCanvasTileUpdated>();
        _net.RegisterNetMessage<MsgCanvasBlobRequest>(OnBlobRequest);
        _net.RegisterNetMessage<MsgCanvasChunk>(OnChunk);
        _net.RegisterNetMessage<MsgCanvasTransferAck>(OnTransferAck);

        InitializeView();

        Subs.CVar(_cfg, ADTCCVars.CanvasDownKbps, v => _downRate = Math.Max(1, v) * 1024.0, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasUpKbps, v => _upRate = Math.Max(1, v) * 1024.0, true);
    }

    #region Отправка

    private void SendManifest(CanvasSession session, ICommonSession player)
    {
        var msg = new MsgCanvasManifest
        {
            Painting = session.PaintingUid,
            Width = session.Width,
            Height = session.Height,
            Layers = session.LayerInfos(),
        };

        for (var i = 0; i < session.Tiles.Length; i++)
        {
            msg.Tiles.Add(new CanvasTileEntry(session.KeyOf(i), session.Tiles[i].Version, session.Tiles[i].Content));
        }

        foreach (var layer in session.Layers)
        {
            for (var i = 0; i < layer.Tiles.Length; i++)
            {
                if (layer.Tiles[i].Version == 0)
                    continue;

                msg.Tiles.Add(new CanvasTileEntry(session.KeyOf(i, layer.Id), layer.Tiles[i].Version, layer.Tiles[i].Content));
            }
        }

        _net.ServerSendMessage(msg, player.Channel);
    }

    private void SendClosed(CanvasSession session, ICommonSession player, CanvasCloseReason reason)
    {
        var msg = new MsgCanvasSessionClosed
        {
            Painting = session.PaintingUid,
            Reason = reason,
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    private void Reply(ICommonSession player, MsgCanvasTileCommit commit, CanvasCommitResult result, CanvasSession? session)
    {
        var msg = new MsgCanvasCommitResult
        {
            Painting = commit.Painting,
            CommitId = commit.CommitId,
            Key = commit.Key,
            Result = result,
        };

        if (session != null && session.TryGetTiles(commit.Key, out var tiles, out var index))
        {
            msg.Version = tiles[index].Version;
            msg.Content = tiles[index].Content;
        }

        _net.ServerSendMessage(msg, player.Channel);
    }

    private static void SetBusy(CanvasSession session, CanvasTileKey key, bool busy)
    {
        if (session.TryGetTiles(key, out var tiles, out var index))
            tiles[index].Busy = busy;
    }

    #endregion

    #region Коммиты

    private void OnTileCommit(MsgCanvasTileCommit msg)
    {
        if (!_players.TryGetSessionByChannel(msg.MsgChannel, out var player))
            return;

        if (msg.Malformed)
        {
            Reply(player, msg, CanvasCommitResult.Invalid, null);
            return;
        }

        if (!_sessions.TryGetValue(msg.Painting, out var session) ||
            session.State == CanvasSessionState.Loading ||
            !session.CanCommit(player.UserId, msg.Key.Layer))
        {
            Reply(player, msg, CanvasCommitResult.NotAllowed, null);
            return;
        }

        if (!session.TryGetTiles(msg.Key, out var tiles, out var index))
        {
            Reply(player, msg, CanvasCommitResult.Invalid, null);
            return;
        }

        if (tiles[index].Busy || session.Submitting)
        {
            Reply(player, msg, CanvasCommitResult.Busy, session);
            return;
        }

        if (msg.BaseVersion != tiles[index].Version)
        {
            Reply(player, msg, CanvasCommitResult.VersionMismatch, session);
            return;
        }

        switch (msg.Content.Kind)
        {
            case CanvasTileKind.Empty:
                Apply(session, msg, CanvasTileContent.Empty, null, player);
                return;

            case CanvasTileKind.Solid:
                Apply(session, msg, CanvasTileContent.Solid(msg.Content.Rgba), null, player);
                return;
        }

        if (msg.Size <= 0 || msg.Size > CanvasConstants.MaxTileBytes)
        {
            Reply(player, msg, CanvasCommitResult.TooLarge, session);
            return;
        }

        var peer = GetPeer(player);
        if (peer.Incoming.TryGetValue(msg.TransferId, out var transfer) && transfer.Complete)
        {
            peer.Incoming.Remove(msg.TransferId);
            ValidateAndApply(session, msg, transfer, player);
            return;
        }

        if (peer.PendingCommits.Count >= MaxPendingPerPeer || peer.PendingCommits.ContainsKey(msg.TransferId))
        {
            Reply(player, msg, CanvasCommitResult.Busy, session);
            return;
        }

        peer.PendingCommits[msg.TransferId] = new CanvasPendingCommit(msg, _timing.RealTime);
        tiles[index].Busy = true;
    }

    private async void ValidateAndApply(CanvasSession session, MsgCanvasTileCommit msg,
        CanvasIncomingTransfer transfer, ICommonSession player)
    {
        try
        {
            if (transfer.Total != msg.Size)
            {
                SetBusy(session, msg.Key, false);
                Reply(player, msg, CanvasCommitResult.Invalid, session);
                return;
            }

            if (_validating >= MaxValidating)
            {
                SetBusy(session, msg.Key, false);
                Reply(player, msg, CanvasCommitResult.Busy, session);
                return;
            }

            if (!session.TryGetIndex(msg.Key, out var index))
            {
                Reply(player, msg, CanvasCommitResult.Invalid, session);
                return;
            }

            SetBusy(session, msg.Key, true);
            _validating++;

            var size = session.TileSize(index);
            var data = transfer.Buffer;
            CanvasValidation result;
            try
            {
                result = await Task.Run(() => CanvasTileValidator.Validate(data, size.X, size.Y));
            }
            finally
            {
                _validating--;
            }

            if (session.Closed)
                return;

            SetBusy(session, msg.Key, false);

            if (!result.Valid)
            {
                Reply(player, msg, CanvasCommitResult.Invalid, session);
                return;
            }

            var blob = result.Content.Kind == CanvasTileKind.Blob ? data : null;
            Apply(session, msg, result.Content, blob, player);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to validate canvas tile {msg.Key} of {msg.Painting}: {e}");
            if (!session.Closed)
            {
                SetBusy(session, msg.Key, false);
                Reply(player, msg, CanvasCommitResult.Invalid, session);
            }
        }
    }

    private void Apply(CanvasSession session, MsgCanvasTileCommit msg, CanvasTileContent content,
        byte[]? blob, ICommonSession player)
    {
        if (!session.TryGetTiles(msg.Key, out var tiles, out var index))
        {
            Reply(player, msg, CanvasCommitResult.NotAllowed, session);
            return;
        }

        ref var tile = ref tiles[index];

        if (session.State == CanvasSessionState.Loading || msg.BaseVersion != tile.Version)
        {
            Reply(player, msg, CanvasCommitResult.VersionMismatch, session);
            return;
        }

        if (!session.CanCommit(player.UserId, msg.Key.Layer) ||
            !session.Members.TryGetValue(player.UserId, out var member))
        {
            Reply(player, msg, CanvasCommitResult.NotAllowed, session);
            return;
        }

        var baseLayer = msg.Key.Layer == CanvasConstants.BaseLayer;
        if (blob != null)
        {
            var oldBytes = tile.Content.Kind == CanvasTileKind.Blob && session.Blobs.TryGetValue(tile.Content.Hash, out var old)
                ? old.Length
                : 0;

            var used = baseLayer ? session.BaseBytes() : session.LayerBytes();
            var limit = baseLayer ? _maxPaintingBytes : _maxLayerBytes;
            if (used - oldBytes + blob.Length > limit)
            {
                Reply(player, msg, CanvasCommitResult.TooLarge, session);
                return;
            }

            session.Blobs[content.Hash] = blob;
        }

        tile.Version++;
        tile.Content = content;

        if (baseLayer)
        {
            session.Changes++;
            session.Contributions[player.UserId] = session.Contributions.GetValueOrDefault(player.UserId) + 1;
        }

        session.DropUnreferencedBlobs();

        Reply(player, msg, CanvasCommitResult.Ok, session);

        var update = new MsgCanvasTileUpdated
        {
            Painting = session.PaintingUid,
            Key = msg.Key,
            Version = tile.Version,
            Content = tile.Content,
            Author = member.Index,
            StrokeIds = msg.StrokeIds,
        };

        foreach (var subscriber in session.Subscribers)
        {
            if (subscriber != player)
                _net.ServerSendMessage(update, subscriber.Channel);
        }
    }

    #endregion

    #region Передачи

    private CanvasPeer GetPeer(ICommonSession player)
    {
        if (_peers.TryGetValue(player.UserId, out var peer))
            return peer;

        var now = _timing.RealTime;
        peer = new CanvasPeer(player, now)
        {
            UpTokens = UpCapacity(),
            DownTokens = DownCapacity(),
        };

        _peers[player.UserId] = peer;
        return peer;
    }

    private double UpCapacity()
    {
        return Math.Max(CanvasConstants.MaxTileBytes * 4.0, _upRate);
    }

    private double DownCapacity()
    {
        return Math.Max(CanvasConstants.ChunkSize * 2.0, _downRate / 2);
    }

    private void Refill(CanvasPeer peer, TimeSpan now)
    {
        var dt = (now - peer.LastRefill).TotalSeconds;
        peer.LastRefill = now;
        if (dt <= 0)
            return;

        peer.UpTokens = Math.Min(UpCapacity(), peer.UpTokens + _upRate * dt);
        peer.DownTokens = Math.Min(DownCapacity(), peer.DownTokens + _downRate * dt);
    }

    private void OnChunk(MsgCanvasChunk msg)
    {
        if (msg.Malformed || !_players.TryGetSessionByChannel(msg.MsgChannel, out var player))
            return;

        var peer = GetPeer(player);
        var now = _timing.RealTime;
        Refill(peer, now);

        if (peer.UpTokens < msg.Data.Length)
        {
            peer.Incoming.Remove(msg.TransferId);
            FailPending(peer, msg.TransferId, CanvasCommitResult.Busy);
            return;
        }

        peer.UpTokens -= msg.Data.Length;

        if (!peer.Incoming.TryGetValue(msg.TransferId, out var transfer))
        {
            var count = (msg.TotalLength + CanvasConstants.ChunkSize - 1) / CanvasConstants.ChunkSize;
            if (msg.TotalLength <= 0 ||
                msg.TotalLength > CanvasConstants.MaxTileBytes ||
                msg.Count != count ||
                peer.Incoming.Count >= MaxIncomingPerPeer)
            {
                FailPending(peer, msg.TransferId, CanvasCommitResult.TooLarge);
                return;
            }

            transfer = new CanvasIncomingTransfer(msg.TransferId, msg.TotalLength, count, now);
            peer.Incoming[msg.TransferId] = transfer;
        }

        if (msg.TotalLength != transfer.Total || msg.Index < 0 || msg.Index >= transfer.Count || transfer.Received[msg.Index])
            return;

        var offset = msg.Index * CanvasConstants.ChunkSize;
        var expected = Math.Min(CanvasConstants.ChunkSize, transfer.Total - offset);
        if (msg.Data.Length != expected)
            return;

        Buffer.BlockCopy(msg.Data, 0, transfer.Buffer, offset, expected);
        transfer.Received[msg.Index] = true;
        transfer.ReceivedCount++;

        if (!transfer.Complete || !peer.PendingCommits.Remove(msg.TransferId, out var pending))
            return;

        peer.Incoming.Remove(msg.TransferId);

        if (!_sessions.TryGetValue(pending.Message.Painting, out var session) || session.Closed)
            return;

        if (session.State == CanvasSessionState.Loading || !session.CanCommit(player.UserId, pending.Message.Key.Layer))
        {
            SetBusy(session, pending.Message.Key, false);
            Reply(player, pending.Message, CanvasCommitResult.NotAllowed, session);
            return;
        }

        ValidateAndApply(session, pending.Message, transfer, player);
    }

    private void FailPending(CanvasPeer peer, uint transferId, CanvasCommitResult result)
    {
        if (!peer.PendingCommits.Remove(transferId, out var pending))
            return;

        _sessions.TryGetValue(pending.Message.Painting, out var session);
        if (session != null)
            SetBusy(session, pending.Message.Key, false);

        Reply(peer.Session, pending.Message, result, session);
    }

    private void OnBlobRequest(MsgCanvasBlobRequest msg)
    {
        if (msg.Malformed || !_players.TryGetSessionByChannel(msg.MsgChannel, out var player))
            return;

        var peer = GetPeer(player);
        if (!_sessions.TryGetValue(msg.Painting, out var session) ||
            !session.Subscribers.Contains(player) && !session.CanCommit(player.UserId))
        {
            TryServeViewBlobs(peer, msg);
            return;
        }

        foreach (var hash in msg.Hashes)
        {
            if (peer.Queued.Contains(hash) || !session.Blobs.TryGetValue(hash, out var data))
                continue;

            peer.Queued.Add(hash);
            peer.Outgoing.Enqueue(new CanvasOutgoingTransfer(peer.NextTransferId++, hash, data));
        }
    }

    private void OnTransferAck(MsgCanvasTransferAck msg)
    {
        if (!_players.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
            !_peers.TryGetValue(player.UserId, out var peer) ||
            !peer.InFlight.Remove(msg.TransferId, out var transfer))
        {
            return;
        }

        peer.InFlightBytes -= transfer.Data.Length;
        peer.Queued.Remove(transfer.Hash);
    }

    private void UpdatePeers(TimeSpan now)
    {
        if (_peers.Count == 0)
            return;

        foreach (var peer in _peers.Values)
        {
            if (peer.Idle)
                continue;

            Refill(peer, now);
            ExpireTransfers(peer, now);
            SendChunks(peer, now);
        }
    }

    private void ExpireTransfers(CanvasPeer peer, TimeSpan now)
    {
        if (peer.Incoming.Count > 0)
        {
            var stale = new List<uint>();
            foreach (var (id, transfer) in peer.Incoming)
            {
                if (now - transfer.Started > TransferTimeout)
                    stale.Add(id);
            }

            foreach (var id in stale)
            {
                peer.Incoming.Remove(id);
            }
        }

        if (peer.PendingCommits.Count > 0)
        {
            var stale = new List<uint>();
            foreach (var (id, pending) in peer.PendingCommits)
            {
                if (now - pending.Started > TransferTimeout)
                    stale.Add(id);
            }

            foreach (var id in stale)
            {
                FailPending(peer, id, CanvasCommitResult.Busy);
            }
        }

        if (peer.InFlight.Count > 0)
        {
            var stale = new List<uint>();
            foreach (var (id, transfer) in peer.InFlight)
            {
                if (transfer != peer.Sending && now - transfer.Started > TransferTimeout)
                    stale.Add(id);
            }

            foreach (var id in stale)
            {
                if (!peer.InFlight.Remove(id, out var transfer))
                    continue;

                peer.InFlightBytes -= transfer.Data.Length;
                peer.Queued.Remove(transfer.Hash);
            }
        }
    }

    private void SendChunks(CanvasPeer peer, TimeSpan now)
    {
        while (true)
        {
            var transfer = peer.Sending;
            if (transfer == null)
            {
                if (peer.Outgoing.Count == 0 ||
                    peer.InFlight.Count >= MaxTransfersInFlight ||
                    peer.InFlightBytes >= MaxBytesInFlight)
                {
                    return;
                }

                transfer = peer.Outgoing.Dequeue();
                transfer.Started = now;
                peer.Sending = transfer;
                peer.InFlight[transfer.Id] = transfer;
                peer.InFlightBytes += transfer.Data.Length;
            }

            var offset = transfer.NextIndex * CanvasConstants.ChunkSize;
            var length = Math.Min(CanvasConstants.ChunkSize, transfer.Data.Length - offset);
            if (peer.DownTokens < length)
                return;

            var data = new byte[length];
            Buffer.BlockCopy(transfer.Data, offset, data, 0, length);

            var msg = new MsgCanvasChunk
            {
                TransferId = transfer.Id,
                Hash = transfer.Hash,
                TotalLength = transfer.Data.Length,
                Index = transfer.NextIndex,
                Count = transfer.Count,
                Data = data,
            };

            _net.ServerSendMessage(msg, peer.Session.Channel);
            peer.DownTokens -= length;
            transfer.NextIndex++;

            if (transfer.NextIndex >= transfer.Count)
            {
                transfer.Started = now;
                peer.Sending = null;
            }
        }
    }

    #endregion
}
