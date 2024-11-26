using Content.Server.Discord;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Text;
using Content.Server.Administration.Managers;
using Robust.Shared.Utility;
using Content.Server.GameTicking;

namespace Content.Server.ADT.Discord.Adminwho;

public sealed class DiscordAdminInfoSenderSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IAdminManager _adminMgr = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private TimeSpan _nextSendTime = TimeSpan.MinValue;
    private readonly TimeSpan _delayInterval = TimeSpan.FromMinutes(15);

    public override void Update(float frameTime)
    {
        if (_time.CurTime < _nextSendTime)
            return;

        _nextSendTime = _time.CurTime + _delayInterval;
        SendAdminInfoToDiscord();
    }

    private async void SendAdminInfoToDiscord()
    {
        var webhookUrl = _cfg.GetCVar(ADTDiscordWebhookCCVars.DiscordAdminwhoWebhook);

        if (string.IsNullOrEmpty(webhookUrl))
            return;

        if (await _discord.GetWebhook(webhookUrl) is not { } webhookData)
            return;

        var sb = new StringBuilder();
        foreach (var admin in _adminMgr.ActiveAdmins)
        {
            var adminData = _adminMgr.GetAdminData(admin)!;
            DebugTools.AssertNotNull(adminData);

            if (adminData.Stealth)
                continue;

            sb.Append(admin.Name);
            if (adminData.Title is { } title)
                sb.Append($": [{title}]");

            sb.AppendLine();
        }

        var serverName = _cfg.GetCVar(CCVars.GameHostName);

        var gameTicker = _entitySystemManager.GetEntitySystem<GameTicker>();
        var round = gameTicker.RunLevel switch
        {
            GameRunLevel.PreRoundLobby => gameTicker.RoundId == 0
                ? "pre-round lobby after server restart"
                : $"pre-round lobby for round {gameTicker.RoundId + 1}",
            GameRunLevel.InRound => $"round {gameTicker.RoundId}",
            GameRunLevel.PostRound => $"post-round {gameTicker.RoundId}",
            _ => throw new ArgumentOutOfRangeException(nameof(gameTicker.RunLevel),
                $"{gameTicker.RunLevel} was not matched."),
        };

        var embed = new WebhookEmbed
        {
            Title = Loc.GetString("title-embed-webhook-adminwho"),
            Description = sb.ToString(),
            Color = 0xff0080,
            Fields = new List<WebhookEmbedField>(),
            Footer = new WebhookEmbedFooter
            {
                Text = $"{serverName} ({round})"
            },
        };

        var payload = new WebhookPayload
        {
            Embeds = new List<WebhookEmbed> { embed },
            Username = Loc.GetString("username-webhook-adminwho")
        };

        var identifier = webhookData.ToIdentifier();
        await _discord.CreateMessage(identifier, payload);
    }
}
