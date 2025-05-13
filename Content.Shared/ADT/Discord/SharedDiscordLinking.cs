// using Lidgren.Network;
// using Robust.Shared.Network;
// using Robust.Shared.Serialization;

// namespace Content.Shared.ADT.Discord
// {
//     // Запрос на проверку состояния привязки к Discord
//     public sealed class MsgRequestDiscordId : NetMessage
//     {
//         public NetUserId UserId { get; set; }

//         public override MsgGroups MsgGroup => MsgGroups.Command;

//         public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
//         {
//             UserId = buffer.ReadUserId();
//         }

//         public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
//         {
//             buffer.Write(UserId);
//         }
//     }

//     // Ответ с Discord ID
//     public sealed class MsgReceiveDiscordId : NetMessage
//     {
//         public NetUserId UserId { get; set; }
//         public string? DiscordId { get; set; }

//         public override MsgGroups MsgGroup => MsgGroups.Command;

//         public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
//         {
//             UserId = buffer.ReadUserId();
//             DiscordId = buffer.ReadString();
//         }

//         public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
//         {
//             buffer.Write(UserId);
//             buffer.Write(DiscordId);
//         }
//     }
// }
