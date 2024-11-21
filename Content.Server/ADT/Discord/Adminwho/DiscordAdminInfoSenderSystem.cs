using Content.Server.Discord;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Configuration;
using System.Text;
using Content.Server.Administration.Managers;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Discord.Adminwho;

public sealed class DiscordAdminInfoSenderSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IAdminManager _adminMgr = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    public TimeSpan NextSecond = TimeSpan.Zero;
    public int SpeakTimerRead = 15;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_time.CurTime >= NextSecond)
        {
            var delay = SpeakTimerRead;
            SendAdminInfoToDiscord();
            NextSecond = _time.CurTime + TimeSpan.FromMinutes(delay);
        }
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

        var embed = new WebhookEmbed
        {
            Title = "Админы на сервере:",
            Description = sb.ToString(),
            Color = 0xff0080,
            Fields = new List<WebhookEmbedField>()
        };

        var username = "Cerberus AdminWho :з";

        var payload = new WebhookPayload
        {
            Embeds = new List<WebhookEmbed> { embed },
            Username = username
        };

        var identifier = webhookData.ToIdentifier();
        await _discord.CreateMessage(identifier, payload);
    }
}
