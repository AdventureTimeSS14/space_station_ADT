using System.Threading.Tasks;
using Content.Client.ADT.Drawing.Canvas;
using Content.Shared.ADT.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Sync;

public readonly record struct CanvasWaitingTile(int Index, uint Version, CanvasTileContent Content);

public readonly record struct CanvasConfirmation(uint Version, byte Author, uint[] StrokeIds);

public readonly record struct CanvasDecodeJob(int Index, uint Version, CanvasTileContent Content, byte[]? Png);

public sealed class CanvasDecodeRun
{
    public readonly CanvasDecodeJob Job;
    public readonly bool FromCache;
    public Task Task = Task.CompletedTask;
    public Rgba32[]? Result;

    public CanvasDecodeRun(CanvasDecodeJob job, bool fromCache)
    {
        Job = job;
        FromCache = fromCache;
    }
}

public sealed class CanvasEncodeRun
{
    public readonly int Index;
    public Task Task = Task.CompletedTask;
    public CanvasTileCommit? Result;

    public CanvasEncodeRun(int index)
    {
        Index = index;
    }
}

public sealed class CanvasClientCommit
{
    public readonly int Index;
    public readonly CanvasTileCommit Commit;
    public readonly uint TransferId;

    public CanvasClientCommit(int index, CanvasTileCommit commit, uint transferId)
    {
        Index = index;
        Commit = commit;
        TransferId = transferId;
    }
}

public sealed class CanvasClientUpload
{
    public readonly uint TransferId;
    public readonly byte[] Data;
    public readonly int Count;
    public int NextIndex;

    public CanvasClientUpload(uint transferId, byte[] data)
    {
        TransferId = transferId;
        Data = data;
        Count = Math.Max(1, (data.Length + CanvasConstants.ChunkSize - 1) / CanvasConstants.ChunkSize);
    }
}

public sealed class CanvasClientDownload
{
    public readonly CanvasHash Hash;
    public readonly byte[] Buffer;
    public readonly bool[] Received;
    public int ReceivedCount;

    public CanvasClientDownload(CanvasHash hash, int total, int count)
    {
        Hash = hash;
        Buffer = new byte[total];
        Received = new bool[count];
    }

    public bool Complete => ReceivedCount == Received.Length;
}

public sealed class CanvasSyncSession
{
    public readonly Guid Painting;
    public readonly CanvasDocument Document;
    public readonly CanvasCollaboration Collaboration;

    public readonly Dictionary<CanvasHash, List<CanvasWaitingTile>> Waiting = new();
    public readonly Dictionary<int, List<CanvasConfirmation>> Confirmations = new();
    public readonly Dictionary<int, int> Rebases = new();
    public readonly Queue<CanvasDecodeJob> DecodeQueue = new();
    public readonly List<CanvasDecodeRun> Decoding = new();
    public readonly List<CanvasEncodeRun> Encoding = new();
    public bool ForceRequest;
    public readonly Dictionary<uint, CanvasClientCommit> Commits = new();
    public readonly Queue<CanvasClientUpload> Uploads = new();

    public int TotalBlobs;
    public int LoadedBlobs;
    public string? Error;
    public bool Detached;
    public TimeSpan DetachDeadline;
    public TimeSpan LastEdit;
    public TimeSpan DirtySince;
    public TimeSpan RetryAfter;
    public TimeSpan NextLayerSave;
    public double UpTokens;
    public TimeSpan LastRefill;
    public uint NextCommitId = 1;
    public uint NextTransferId = 1;

    public CanvasSyncSession(Guid painting, CanvasDocument document, CanvasCollaboration collaboration)
    {
        Painting = painting;
        Document = document;
        Collaboration = collaboration;
    }

    public bool Loaded => Waiting.Count == 0 && DecodeQueue.Count == 0 && Decoding.Count == 0;
}
