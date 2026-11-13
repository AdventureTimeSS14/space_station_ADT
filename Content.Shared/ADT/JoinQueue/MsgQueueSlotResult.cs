using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.JoinQueue;

public sealed class MsgQueueSlotResult : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public bool Won;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Won = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Won);
    }
}
