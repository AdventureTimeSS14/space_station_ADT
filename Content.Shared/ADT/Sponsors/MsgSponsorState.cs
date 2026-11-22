using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorStatePayload
{
    public SponsorBenefits Benefits = new();
    public SponsorTierSummary[] Tiers = Array.Empty<SponsorTierSummary>();
    public DateTime? NextExpiry;

    public Color? SelectedOocColor;

    public Color? SelectedGhostColor;
}

public sealed class MsgSponsorState : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public SponsorStatePayload? State;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var hasState = buffer.ReadBoolean();
        buffer.ReadPadBits();

        if (!hasState)
        {
            State = null;
            return;
        }

        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream();
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out SponsorStatePayload state);
        State = state;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(State != null);
        buffer.WritePadBits();

        if (State == null)
            return;

        var stream = new MemoryStream();
        serializer.SerializeDirect(stream, State);
        buffer.WriteVariableInt32((int) stream.Length);
        buffer.Write(stream.AsSpan());
    }
}
