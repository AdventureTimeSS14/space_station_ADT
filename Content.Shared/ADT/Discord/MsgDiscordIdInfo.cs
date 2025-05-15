using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Discord;

/// <summary>
/// Сервер отправляет Discord ID и UserName клиенту
/// </summary>
public sealed class MsgDiscordIdInfo : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public NetUserId UserId;
    public string? DiscordId;
    public string? DiscordUsername;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var guid = buffer.ReadGuid();
        UserId = new NetUserId(guid);

        var hasId = buffer.ReadBoolean();
        DiscordId = hasId ? buffer.ReadString() : null;

        var hasUsername = buffer.ReadBoolean();
        DiscordUsername = hasUsername ? buffer.ReadString() : null;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(UserId.UserId);
        buffer.Write(DiscordId != null);
        if (DiscordId != null)
            buffer.Write(DiscordId);

        buffer.Write(DiscordUsername != null);
        if (DiscordUsername != null)
            buffer.Write(DiscordUsername);
    }
}
