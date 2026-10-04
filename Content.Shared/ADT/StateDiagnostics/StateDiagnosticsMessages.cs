using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StateDiagnostics;

public sealed class MsgStateDiagnosticsRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Requests;
    public float Window;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Requests = buffer.ReadVariableInt32();
        Window = buffer.ReadFloat();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Requests);
        buffer.Write(Window);
    }
}

public sealed class MsgStateDiagnosticsReport : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string Report = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Report = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Report);
    }
}
