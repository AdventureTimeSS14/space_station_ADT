using Content.Shared.ADT.Drawing;
using Robust.Shared.Network;

namespace Content.Client.ADT.Drawing.Sync;

public sealed class CanvasNetManager
{
    [Dependency] private readonly IClientNetManager _net = default!;

    public event Action<MsgCanvasManifest>? ManifestReceived;
    public event Action<MsgCanvasSessionClosed>? SessionClosed;
    public event Action<MsgCanvasCommitResult>? CommitResult;
    public event Action<MsgCanvasTileUpdated>? TileUpdated;
    public event Action<MsgCanvasChunk>? ChunkReceived;
    public event Action<MsgCanvasViewInfo>? ViewInfoReceived;
    public event Action<MsgCanvasInvalidate>? InvalidateReceived;
    public event Action<MsgCanvasStroke>? StrokeReceived;
    public event Action<MsgCanvasCursor>? CursorReceived;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgCanvasStroke>(m => StrokeReceived?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasCursor>(m => CursorReceived?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasManifest>(m => ManifestReceived?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasSessionClosed>(m => SessionClosed?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasTileCommit>();
        _net.RegisterNetMessage<MsgCanvasCommitResult>(m => CommitResult?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasTileUpdated>(m => TileUpdated?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasBlobRequest>();
        _net.RegisterNetMessage<MsgCanvasChunk>(m => ChunkReceived?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasTransferAck>();
        _net.RegisterNetMessage<MsgCanvasViewRequest>();
        _net.RegisterNetMessage<MsgCanvasViewInfo>(m => ViewInfoReceived?.Invoke(m));
        _net.RegisterNetMessage<MsgCanvasInvalidate>(m => InvalidateReceived?.Invoke(m));
    }

    public void Send(NetMessage message)
    {
        _net.ClientSendMessage(message);
    }
}
