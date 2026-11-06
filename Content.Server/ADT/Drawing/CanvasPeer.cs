using Content.Shared.ADT.Drawing;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public sealed class CanvasIncomingTransfer
{
    public readonly uint Id;
    public readonly int Total;
    public readonly int Count;
    public readonly byte[] Buffer;
    public readonly bool[] Received;
    public readonly TimeSpan Started;
    public int ReceivedCount;

    public CanvasIncomingTransfer(uint id, int total, int count, TimeSpan started)
    {
        Id = id;
        Total = total;
        Count = count;
        Buffer = new byte[total];
        Received = new bool[count];
        Started = started;
    }

    public bool Complete => ReceivedCount == Count;
}

public sealed class CanvasPendingCommit
{
    public readonly MsgCanvasTileCommit Message;
    public readonly int Index;
    public readonly TimeSpan Started;

    public CanvasPendingCommit(MsgCanvasTileCommit message, int index, TimeSpan started)
    {
        Message = message;
        Index = index;
        Started = started;
    }
}

public sealed class CanvasOutgoingTransfer
{
    public readonly uint Id;
    public readonly CanvasHash Hash;
    public readonly byte[] Data;
    public readonly int Count;
    public int NextIndex;
    public TimeSpan Started;

    public CanvasOutgoingTransfer(uint id, CanvasHash hash, byte[] data)
    {
        Id = id;
        Hash = hash;
        Data = data;
        Count = Math.Max(1, (data.Length + CanvasConstants.ChunkSize - 1) / CanvasConstants.ChunkSize);
    }
}

public sealed class CanvasPeer
{
    public readonly ICommonSession Session;
    public readonly Dictionary<uint, CanvasIncomingTransfer> Incoming = new();
    public readonly Dictionary<uint, CanvasPendingCommit> PendingCommits = new();
    public readonly Queue<CanvasOutgoingTransfer> Outgoing = new();
    public readonly Dictionary<uint, CanvasOutgoingTransfer> InFlight = new();
    public readonly HashSet<CanvasHash> Queued = new();
    public readonly HashSet<Guid> Views = new();

    public CanvasOutgoingTransfer? Sending;
    public long InFlightBytes;
    public double UpTokens;
    public double DownTokens;
    public TimeSpan LastRefill;
    public uint NextTransferId = 1;
    public int ViewLoads;

    public CanvasPeer(ICommonSession session, TimeSpan now)
    {
        Session = session;
        LastRefill = now;
    }

    public bool Idle =>
        Incoming.Count == 0 &&
        PendingCommits.Count == 0 &&
        Outgoing.Count == 0 &&
        InFlight.Count == 0 &&
        Sending == null;

    public void ClearOutgoing()
    {
        Outgoing.Clear();
        InFlight.Clear();
        Queued.Clear();
        Sending = null;
        InFlightBytes = 0;
    }
}
