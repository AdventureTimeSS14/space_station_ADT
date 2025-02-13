using System.Text;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Export;

public sealed class ExportYmlMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public string Data { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        // Read the length of the byte array
        int length = buffer.ReadInt32();
        // Read the byte array
        byte[] dataBytes = buffer.ReadBytes(length);
        // Decode the byte array to a string using UTF-8 encoding
        Data = Encoding.UTF8.GetString(dataBytes);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        // Encode the string to a byte array using UTF-8 encoding
        byte[] dataBytes = Encoding.UTF8.GetBytes(Data);
        // Write the length of the byte array
        buffer.Write(dataBytes.Length);
        // Write the byte array
        buffer.Write(dataBytes);
    }
}
