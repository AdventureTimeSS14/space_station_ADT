using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

public enum CanvasCommitResult : byte
{
    Ok,
    VersionMismatch,
    Invalid,
    Busy,
    TooLarge,
    NotAllowed,
}

public enum CanvasCloseReason : byte
{
    Closed,
    HostAway,
    Removed,
    Error,
    Submitted,
    Kicked,
    Left,
}

public static class CanvasNetChannels
{
    public const int Session = 7;
    public const int Cursor = 8;
}

public readonly record struct CanvasTileEntry(CanvasTileKey Key, uint Version, CanvasTileContent Content);

public static class CanvasNetIO
{
    public static void WriteHash(NetOutgoingMessage buffer, CanvasHash hash)
    {
        buffer.Write(hash.High);
        buffer.Write(hash.Low);
    }

    public static CanvasHash ReadHash(NetIncomingMessage buffer)
    {
        var high = buffer.ReadUInt64();
        var low = buffer.ReadUInt64();
        return new CanvasHash(high, low);
    }

    public static void WriteKey(NetOutgoingMessage buffer, CanvasTileKey key)
    {
        buffer.Write(key.Layer);
        buffer.Write(key.X);
        buffer.Write(key.Y);
    }

    public static CanvasTileKey ReadKey(NetIncomingMessage buffer)
    {
        var layer = buffer.ReadByte();
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        return new CanvasTileKey(layer, x, y);
    }

    public static void WriteContent(NetOutgoingMessage buffer, CanvasTileContent content)
    {
        buffer.Write((byte) content.Kind);
        switch (content.Kind)
        {
            case CanvasTileKind.Solid:
                buffer.Write(content.Rgba);
                break;

            case CanvasTileKind.Blob:
                WriteHash(buffer, content.Hash);
                break;
        }
    }

    public static CanvasTileContent ReadContent(NetIncomingMessage buffer)
    {
        var kind = (CanvasTileKind) buffer.ReadByte();
        switch (kind)
        {
            case CanvasTileKind.Solid:
                return CanvasTileContent.Solid(buffer.ReadUInt32());

            case CanvasTileKind.Blob:
                return CanvasTileContent.Blob(ReadHash(buffer));

            default:
                return CanvasTileContent.Empty;
        }
    }

    public static bool HasBits(NetIncomingMessage buffer, long bytes)
    {
        return bytes >= 0 && bytes * 8 <= buffer.LengthBits - buffer.Position;
    }

    public static void WriteIds(NetOutgoingMessage buffer, uint[] ids)
    {
        buffer.WriteVariableInt32(ids.Length);
        foreach (var id in ids)
        {
            buffer.Write(id);
        }
    }

    public static bool TryReadIds(NetIncomingMessage buffer, out uint[] ids)
    {
        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > CanvasConstants.MaxStrokeIds || !HasBits(buffer, count * 4L))
        {
            ids = Array.Empty<uint>();
            return false;
        }

        ids = new uint[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = buffer.ReadUInt32();
        }

        return true;
    }
}

public sealed class MsgCanvasManifest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public int Width;
    public int Height;
    public List<CanvasTileEntry> Tiles = new();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Width = buffer.ReadUInt16();
        Height = buffer.ReadUInt16();

        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > CanvasConstants.MaxTiles)
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
            return;
        }

        Tiles = new List<CanvasTileEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var key = CanvasNetIO.ReadKey(buffer);
            var version = buffer.ReadUInt32();
            var content = CanvasNetIO.ReadContent(buffer);
            Tiles.Add(new CanvasTileEntry(key, version, content));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write((ushort) Width);
        buffer.Write((ushort) Height);
        buffer.WriteVariableInt32(Tiles.Count);
        foreach (var tile in Tiles)
        {
            CanvasNetIO.WriteKey(buffer, tile.Key);
            buffer.Write(tile.Version);
            CanvasNetIO.WriteContent(buffer, tile.Content);
        }
    }
}

public sealed class MsgCanvasSessionClosed : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public CanvasCloseReason Reason;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Reason = (CanvasCloseReason) buffer.ReadByte();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write((byte) Reason);
    }
}

public sealed class MsgCanvasTileCommit : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Painting;
    public uint CommitId;
    public CanvasTileKey Key;
    public uint BaseVersion;
    public CanvasTileContent Content;
    public uint TransferId;
    public int Size;
    public uint[] StrokeIds = Array.Empty<uint>();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        CommitId = buffer.ReadUInt32();
        Key = CanvasNetIO.ReadKey(buffer);
        BaseVersion = buffer.ReadUInt32();
        Content = CanvasNetIO.ReadContent(buffer);
        TransferId = buffer.ReadUInt32();
        Size = buffer.ReadVariableInt32();

        if (!CanvasNetIO.TryReadIds(buffer, out StrokeIds))
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write(CommitId);
        CanvasNetIO.WriteKey(buffer, Key);
        buffer.Write(BaseVersion);
        CanvasNetIO.WriteContent(buffer, Content);
        buffer.Write(TransferId);
        buffer.WriteVariableInt32(Size);
        CanvasNetIO.WriteIds(buffer, StrokeIds);
    }
}

public sealed class MsgCanvasCommitResult : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public uint CommitId;
    public CanvasTileKey Key;
    public CanvasCommitResult Result;
    public uint Version;
    public CanvasTileContent Content;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        CommitId = buffer.ReadUInt32();
        Key = CanvasNetIO.ReadKey(buffer);
        Result = (CanvasCommitResult) buffer.ReadByte();
        Version = buffer.ReadUInt32();
        Content = CanvasNetIO.ReadContent(buffer);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write(CommitId);
        CanvasNetIO.WriteKey(buffer, Key);
        buffer.Write((byte) Result);
        buffer.Write(Version);
        CanvasNetIO.WriteContent(buffer, Content);
    }
}

public sealed class MsgCanvasTileUpdated : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public CanvasTileKey Key;
    public uint Version;
    public CanvasTileContent Content;
    public byte Author = CanvasConstants.NoAuthor;
    public uint[] StrokeIds = Array.Empty<uint>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Key = CanvasNetIO.ReadKey(buffer);
        Version = buffer.ReadUInt32();
        Content = CanvasNetIO.ReadContent(buffer);
        Author = buffer.ReadByte();

        if (!CanvasNetIO.TryReadIds(buffer, out StrokeIds))
            buffer.Position = buffer.LengthBits;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        CanvasNetIO.WriteKey(buffer, Key);
        buffer.Write(Version);
        CanvasNetIO.WriteContent(buffer, Content);
        buffer.Write(Author);
        CanvasNetIO.WriteIds(buffer, StrokeIds);
    }
}

public sealed class MsgCanvasBlobRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Painting;
    public List<CanvasHash> Hashes = new();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();

        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > CanvasConstants.MaxBlobRequest)
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
            return;
        }

        Hashes = new List<CanvasHash>(count);
        for (var i = 0; i < count; i++)
        {
            Hashes.Add(CanvasNetIO.ReadHash(buffer));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.WriteVariableInt32(Hashes.Count);
        foreach (var hash in Hashes)
        {
            CanvasNetIO.WriteHash(buffer, hash);
        }
    }
}

public sealed class MsgCanvasChunk : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public uint TransferId;
    public CanvasHash Hash;
    public int TotalLength;
    public int Index;
    public int Count;
    public byte[] Data = Array.Empty<byte>();
    public bool Malformed;

    public override int EstimateBufferSize()
    {
        return 48 + Data.Length;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        TransferId = buffer.ReadUInt32();
        Hash = CanvasNetIO.ReadHash(buffer);
        TotalLength = buffer.ReadVariableInt32();
        Index = buffer.ReadUInt16();
        Count = buffer.ReadUInt16();

        var length = buffer.ReadVariableInt32();
        if (length < 0 || length > CanvasConstants.ChunkSize || !CanvasNetIO.HasBits(buffer, length))
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
            return;
        }

        Data = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(TransferId);
        CanvasNetIO.WriteHash(buffer, Hash);
        buffer.WriteVariableInt32(TotalLength);
        buffer.Write((ushort) Index);
        buffer.Write((ushort) Count);
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
    }
}

public sealed class MsgCanvasTransferAck : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public uint TransferId;
    public bool Ok;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        TransferId = buffer.ReadUInt32();
        Ok = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(TransferId);
        buffer.Write(Ok);
    }
}

public readonly record struct CanvasLevelTile(byte Lod, CanvasTileKey Key, CanvasTileContent Content);

public sealed class MsgCanvasViewRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Painting;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
    }
}

public sealed class MsgCanvasViewInfo : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Painting;
    public bool Allowed;
    public PaintingStatus Status;
    public int Width;
    public int Height;
    public string Title = string.Empty;
    public CanvasTileContent Thumb;
    public List<CanvasLevelTile> Tiles = new();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Allowed = buffer.ReadBoolean();
        if (!Allowed)
            return;

        Status = (PaintingStatus) buffer.ReadByte();
        Width = buffer.ReadUInt16();
        Height = buffer.ReadUInt16();
        Title = buffer.ReadString();
        if (Title.Length > CanvasConstants.MaxTitleLength)
            Title = Title.Substring(0, CanvasConstants.MaxTitleLength);

        Thumb = CanvasNetIO.ReadContent(buffer);

        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > CanvasConstants.MaxTiles * 2)
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
            return;
        }

        Tiles = new List<CanvasLevelTile>(count);
        for (var i = 0; i < count; i++)
        {
            var lod = buffer.ReadByte();
            var key = CanvasNetIO.ReadKey(buffer);
            var content = CanvasNetIO.ReadContent(buffer);
            Tiles.Add(new CanvasLevelTile(lod, key, content));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write(Allowed);
        if (!Allowed)
            return;

        buffer.Write((byte) Status);
        buffer.Write((ushort) Width);
        buffer.Write((ushort) Height);
        buffer.Write(Title);
        CanvasNetIO.WriteContent(buffer, Thumb);
        buffer.WriteVariableInt32(Tiles.Count);
        foreach (var tile in Tiles)
        {
            buffer.Write(tile.Lod);
            CanvasNetIO.WriteKey(buffer, tile.Key);
            CanvasNetIO.WriteContent(buffer, tile.Content);
        }
    }
}

public sealed class MsgCanvasInvalidate : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Painting;
    public List<CanvasHash> Hashes = new();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();

        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > CanvasConstants.MaxTiles * 2 + 1)
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
            return;
        }

        Hashes = new List<CanvasHash>(count);
        for (var i = 0; i < count; i++)
        {
            Hashes.Add(CanvasNetIO.ReadHash(buffer));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.WriteVariableInt32(Hashes.Count);
        foreach (var hash in Hashes)
        {
            CanvasNetIO.WriteHash(buffer, hash);
        }
    }
}
