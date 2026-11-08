using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StationAi;

public sealed class MsgAiEyeTeleport : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public NetEntity Target;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Target = buffer.ReadNetEntity();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Target);
    }
}
