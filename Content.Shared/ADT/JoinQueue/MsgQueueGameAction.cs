using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.JoinQueue;

public sealed class MsgQueueGameAction : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public QueueGameAction Action;

    public byte Cell;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Action = (QueueGameAction) buffer.ReadByte();
        Cell = buffer.ReadByte();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write((byte) Action);
        buffer.Write(Cell);
    }
}
