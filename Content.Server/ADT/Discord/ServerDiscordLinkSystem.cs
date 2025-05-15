// using Content.Shared.ADT.Discord;
// using Robust.Shared.Network;
// using Content.Server.Database;
// using Robust.Server.Player;

// namespace Content.Server.ADT.Discord;

// public sealed class ServerDiscordLinkSystem : EntitySystem
// {
//     [Dependency] private readonly IServerNetManager _net = default!;
//     [Dependency] private readonly IServerDbManager _db = default!;
//     [Dependency] private readonly IPlayerManager _player = default!;

//     public override void Initialize()
//     {
//         base.Initialize();
//         _net.RegisterNetMessage<MsgRequestDiscordId>(HandleRequest);
//     }

//     private void HandleRequest(MsgRequestDiscordId msg)
//     {
//         var discordId = _db.GetDiscordIdAsync(msg.UserId.UserId).GetAwaiter().GetResult();

//         var response = new MsgReceiveDiscordId
//         {
//             UserId = msg.UserId,
//             DiscordId = discordId
//         };

//         var session = _player.GetSessionById(msg.UserId);
//         if (session != null)
//         {
//             var channel = session.Channel;
//             _net.ServerSendMessage(response, channel);
//         }
//     }
// }
