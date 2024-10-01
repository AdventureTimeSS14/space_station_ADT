using Content.Server.Discord;

namespace Content.Server.ADT.Discord.Bans;

public interface IDiscordBanPayloadGenerator
{
    WebhookPayload Generate(BanInfo info);
}
