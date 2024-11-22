// using System.Linq;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Content.Server.Discord;
// using Content.Server.GameTicking;
// using Content.Shared.CCVar;
// using Content.Shared.Roles;
// using Robust.Shared.Configuration;
// using Robust.Shared.Prototypes;
// using Content.Shared.ADT.CCVar;

// namespace Content.Server.ADT.Discord.Adminchat;

// public sealed class DiscordChatInfoSender : IDiscordChatInfoSender
// {
//     [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
//     [Dependency] private readonly IConfigurationManager _cfg = default!;
//     [Dependency] private readonly IPrototypeManager _protoManager = default!;
//     [Dependency] private readonly DiscordWebhook _discord = default!;

//     public async Task SendChatInfoAsync<TGenerator>(ChatInfo info)
//         where TGenerator : IDiscordChatInfoPayloadGenerator, new()
//     {
//         var webhookUrl = _cfg.GetCVar(ADTDiscordWebhookCCVars.DiscordAdminchatWebhook);

//         if (string.IsNullOrEmpty(webhookUrl))
//             return;

//         if (await _discord.GetWebhook(webhookUrl) is not { } webhookData)
//             return;

//         // AddAdditionalInfo(info);
//         // LocalizeAdditionalInfo(info);

//         var identifier = webhookData.ToIdentifier();

//         var payload = new TGenerator().Generate(info);

//         await _discord.CreateMessage(identifier, payload);
//     }
// }
