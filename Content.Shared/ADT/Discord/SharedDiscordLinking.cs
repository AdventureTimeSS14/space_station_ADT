// using System.IO;
// using Lidgren.Network;
// using Robust.Shared.Network;
// using Robust.Shared.Serialization;

// namespace Content.Shared.ADT.Discord;

// /// <summary>
// /// Клиент -> Сервер: Запросить Discord ID по NetUserId
// /// </summary>
// public sealed class MsgRequestDiscordId : NetMessage
// {
//     public override MsgGroups MsgGroup => MsgGroups.Command;
//     public NetUserId UserId;

//     public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
//     {
//         // Считаем все байты из сообщения, обернём в MemoryStream
//         using var mem = new MemoryStream(buffer.ReadBytes(buffer.LengthBytes));
//         UserId = serializer.Deserialize<NetUserId>(mem);
//     }

//     public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
//     {
//         using var mem = new MemoryStream();
//         serializer.Serialize(mem, UserId);
//         buffer.Write(mem.ToArray());
//     }

//     public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
// }

// /// <summary>
// /// Сервер -> Клиент: Отправка Discord ID
// /// </summary>
// public sealed class MsgReceiveDiscordId : NetMessage
// {
//     public override MsgGroups MsgGroup => MsgGroups.Command;
//     public NetUserId UserId;
//     public string? DiscordId;

//     public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
//     {
//         using var mem = new MemoryStream(buffer.ReadBytes(buffer.LengthBytes));
//         UserId = serializer.Deserialize<NetUserId>(mem);
//         DiscordId = serializer.Deserialize<string?>(mem);
//     }

//     public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
//     {
//         using var mem = new MemoryStream();
//         serializer.Serialize(mem, UserId);
//         serializer.Serialize(mem, DiscordId!);
//         buffer.Write(mem.ToArray());
//     }

//     public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
// }
