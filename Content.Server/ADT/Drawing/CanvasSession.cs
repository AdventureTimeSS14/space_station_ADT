using Content.Shared.ADT.Drawing;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public enum CanvasSessionState : byte
{
    Loading,
    Active,
    HostAway,
}

public struct CanvasSessionTile
{
    public uint Version;
    public CanvasTileContent Content;
    public bool Busy;
}

public sealed class CanvasMember
{
    public readonly NetUserId User;
    public readonly byte Index;
    public string Name;
    public CanvasRole Role;
    public ICommonSession? Player;
    public TimeSpan AwayUntil;
    public readonly HashSet<uint> OpenStrokes = new();
    public double SampleTokens;
    public double CursorTokens;
    public TimeSpan LastRefill;
    public long DroppedSamples;

    public CanvasMember(NetUserId user, byte index, string name, CanvasRole role)
    {
        User = user;
        Index = index;
        Name = name;
        Role = role;
    }

    public bool Present => Player != null;

    public bool CanDraw => Role != CanvasRole.Viewer;
}

public sealed class CanvasJoinRequest
{
    public readonly bool Invite;
    public readonly TimeSpan Expires;
    public bool Checking;

    public CanvasJoinRequest(bool invite, TimeSpan expires)
    {
        Invite = invite;
        Expires = expires;
    }
}

public sealed class CanvasSession
{
    public readonly Guid PaintingUid;
    public readonly int Width;
    public readonly int Height;
    public readonly int TilesX;
    public readonly int TilesY;
    public readonly CanvasSessionTile[] Tiles;
    public readonly Dictionary<CanvasHash, byte[]> Blobs = new();
    public readonly HashSet<CanvasHash> PersistedBlobs = new();
    public readonly HashSet<ICommonSession> Subscribers = new();
    public readonly Dictionary<NetUserId, CanvasMember> Members = new();
    public readonly Dictionary<NetUserId, CanvasJoinRequest> Requests = new();
    public readonly Dictionary<NetUserId, int> Contributions = new();
    public CanvasTileContent[] SavedTiles;

    public EntityUid Easel;
    public EntityUid Canvas;
    public NetUserId Host;
    public CanvasSessionState State = CanvasSessionState.Loading;
    public TimeSpan AwayUntil;
    public TimeSpan NextAutosave;
    public int Changes;
    public int SavedChanges;
    public bool Saving;
    public bool Closed;
    public bool Submitting;
    public byte NextIndex;

    public CanvasSession(Guid paintingUid, int width, int height)
    {
        PaintingUid = paintingUid;
        Width = width;
        Height = height;
        TilesX = CanvasConstants.TileCount(width);
        TilesY = CanvasConstants.TileCount(height);
        Tiles = new CanvasSessionTile[TilesX * TilesY];
        SavedTiles = new CanvasTileContent[Tiles.Length];
    }

    public bool Dirty => Changes != SavedChanges;

    public int EditorCount()
    {
        var count = 0;
        foreach (var member in Members.Values)
        {
            if (member.CanDraw)
                count++;
        }

        return count;
    }

    public bool CanCommit(NetUserId user)
    {
        return Members.TryGetValue(user, out var member) && member.CanDraw;
    }

    public bool TryAllocateIndex(out byte index)
    {
        for (var attempt = 0; attempt < CanvasConstants.NoAuthor; attempt++)
        {
            index = NextIndex;
            NextIndex = (byte) ((NextIndex + 1) % CanvasConstants.NoAuthor);

            var used = false;
            foreach (var member in Members.Values)
            {
                if (member.Index == index)
                {
                    used = true;
                    break;
                }
            }

            if (!used)
                return true;
        }

        index = CanvasConstants.NoAuthor;
        return false;
    }

    public bool TryGetIndex(CanvasTileKey key, out int index)
    {
        index = -1;
        if (key.Layer != 0 || key.X >= TilesX || key.Y >= TilesY)
            return false;

        index = key.Y * TilesX + key.X;
        return true;
    }

    public CanvasTileKey KeyOf(int index)
    {
        return new CanvasTileKey(0, (ushort) (index % TilesX), (ushort) (index / TilesX));
    }

    public Vector2i TileSize(int index)
    {
        return new Vector2i(
            CanvasConstants.TileExtent(Width, index % TilesX),
            CanvasConstants.TileExtent(Height, index / TilesX));
    }

    public long ReferencedBytes()
    {
        long total = 0;
        foreach (var hash in ReferencedHashes())
        {
            if (Blobs.TryGetValue(hash, out var data))
                total += data.Length;
        }

        return total;
    }

    public HashSet<CanvasHash> ReferencedHashes()
    {
        var result = new HashSet<CanvasHash>();
        foreach (var tile in Tiles)
        {
            if (tile.Content.Kind == CanvasTileKind.Blob)
                result.Add(tile.Content.Hash);
        }

        return result;
    }

    public void DropUnreferencedBlobs()
    {
        var referenced = ReferencedHashes();
        foreach (var content in SavedTiles)
        {
            if (content.Kind == CanvasTileKind.Blob)
                referenced.Add(content.Hash);
        }

        var unused = new List<CanvasHash>();
        foreach (var hash in Blobs.Keys)
        {
            if (!referenced.Contains(hash))
                unused.Add(hash);
        }

        foreach (var hash in unused)
        {
            Blobs.Remove(hash);
        }
    }
}
