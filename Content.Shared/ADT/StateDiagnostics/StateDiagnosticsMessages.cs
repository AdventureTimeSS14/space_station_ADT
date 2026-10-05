using System.Text;
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

    private const int MaxReportBytes = StateDiagnosticsHelper.MaxReportLength * 3;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var byteLength = (long) buffer.ReadVariableUInt32();

        if (byteLength * 8 > buffer.LengthBits - buffer.Position)
        {
            buffer.Position = buffer.LengthBits;
            Report = string.Empty;
            return;
        }

        var readLength = (int) Math.Min(byteLength, MaxReportBytes);
        Report = Encoding.UTF8.GetString(buffer.ReadBytes(readLength));
        buffer.Position += (byteLength - readLength) * 8;

        if (Report.Length > StateDiagnosticsHelper.MaxReportLength)
            Report = Report.Substring(0, StateDiagnosticsHelper.MaxReportLength);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Report);
    }
}
