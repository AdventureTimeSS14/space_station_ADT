// using Content.Shared.ADT.Discord;
// using Robust.Shared.Network;
// using Content.Server.Database;

// namespace Content.Server.ADT.Discord
// {
//     public sealed class ServerDiscordLinkSystem : EntitySystem
//     {
//         [Dependency] private readonly IServerNetManager _net = default!;
//         [Dependency] private readonly IServerDbManager _db = default!;

//         public override void Initialize()
//         {
//             base.Initialize();
//             _net.RegisterNetMessage<MsgRequestDiscordId>(HandleRequest);
//         }

//         private async void HandleRequest(MsgRequestDiscordId msg, EntitySessionEventArgs args)
//         {
//             var discordId = await _db.GetDiscordIdAsync(msg.UserId.UserId);

//             var response = new MsgReceiveDiscordId
//             {
//                 UserId = msg.UserId,
//                 DiscordId = discordId
//             };

//             _net.ServerSendMessage(response, args.SenderSession.ConnectedClient);
//         }
//     }
// }
