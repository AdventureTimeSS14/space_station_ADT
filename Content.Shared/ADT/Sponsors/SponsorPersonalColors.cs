using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorPersonalColors
{
    public Color? Ooc;

    public Color? Ghost;

    public SponsorPersonalColors Clone()
    {
        return new SponsorPersonalColors
        {
            Ooc = Ooc,
            Ghost = Ghost,
        };
    }
}

public sealed class MsgSetSponsorColors : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public SponsorPersonalColors Colors = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream();
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out SponsorPersonalColors colors);
        Colors = colors;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var stream = new MemoryStream();
        serializer.SerializeDirect(stream, Colors);
        buffer.WriteVariableInt32((int) stream.Length);
        buffer.Write(stream.AsSpan());
    }
}
