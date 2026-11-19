using System.Text;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

public readonly record struct CanvasLayerInfo(
    byte Id,
    NetUserId Owner,
    int LocalId,
    string Name,
    float Opacity,
    bool Visible)
{
    public static byte PackOpacity(float opacity)
    {
        if (!float.IsFinite(opacity))
            return 255;

        return (byte) MathF.Round(Math.Clamp(opacity, 0f, 1f) * 255f);
    }

    public static string SanitizeName(string name)
    {
        var builder = new StringBuilder(Math.Min(name.Length, CanvasConstants.MaxLayerNameLength));
        foreach (var c in name)
        {
            if (builder.Length >= CanvasConstants.MaxLayerNameLength)
                break;

            builder.Append(char.IsControl(c) ? ' ' : c);
        }

        return builder.ToString().Trim();
    }
}

public enum CanvasLayerActionKind : byte
{
    Create,
    Rename,
    Opacity,
    Visible,
    Move,
    Delete,
}

public sealed class MsgCanvasLayers : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public List<CanvasLayerInfo> Layers = new();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        if (!CanvasNetIO.TryReadLayers(buffer, out Layers))
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        CanvasNetIO.WriteLayers(buffer, Layers);
    }
}

public sealed class MsgCanvasLayerAction : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public CanvasLayerActionKind Action;
    public byte Layer;
    public int LocalId;
    public string Name = string.Empty;
    public float Opacity = 1f;
    public bool Visible = true;
    public sbyte Delta;
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Action = (CanvasLayerActionKind) buffer.ReadByte();
        Layer = buffer.ReadByte();
        LocalId = buffer.ReadVariableInt32();
        Name = CanvasLayerInfo.SanitizeName(buffer.ReadString());
        Opacity = buffer.ReadByte() / 255f;
        Visible = buffer.ReadBoolean();
        Delta = buffer.ReadSByte();

        if (Action > CanvasLayerActionKind.Delete)
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write((byte) Action);
        buffer.Write(Layer);
        buffer.WriteVariableInt32(LocalId);
        buffer.Write(Name);
        buffer.Write(CanvasLayerInfo.PackOpacity(Opacity));
        buffer.Write(Visible);
        buffer.Write(Delta);
    }
}

public enum CanvasFlushPhase : byte
{
    Request,
    Flush,
    Done,
    Ready,
}

public sealed class MsgCanvasFlush : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public CanvasFlushPhase Phase;
    public uint Id;
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Phase = (CanvasFlushPhase) buffer.ReadByte();
        Id = buffer.ReadUInt32();

        if (Phase > CanvasFlushPhase.Ready)
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write((byte) Phase);
        buffer.Write(Id);
    }
}
