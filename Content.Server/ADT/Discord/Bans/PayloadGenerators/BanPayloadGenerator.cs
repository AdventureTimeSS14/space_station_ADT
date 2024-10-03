using Content.Server.Discord;

namespace Content.Server.ADT.Discord.Bans.PayloadGenerators;

public abstract class BanPayloadGenerator : IDiscordBanPayloadGenerator
{
    protected WebhookEmbedFooter Footer { get; set; }

    public abstract WebhookPayload Generate(BanInfo info);

    protected virtual void InitializeFooter(BanInfo info)
    {
        var serverName = info.AdditionalInfo.ContainsKey("serverName")
            ? info.AdditionalInfo["serverName"]
            : string.Empty;

        var round = info.AdditionalInfo.ContainsKey("round") ? info.AdditionalInfo["round"] : string.Empty;

        Footer = new WebhookEmbedFooter { Text = $"{serverName} ({round})" };
    }
}
