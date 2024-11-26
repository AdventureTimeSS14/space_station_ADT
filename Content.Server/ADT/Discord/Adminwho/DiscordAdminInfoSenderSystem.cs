using Content.Server.Discord;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Text;
using Content.Server.Administration.Managers;
using Robust.Shared.Utility;
using Content.Server.GameTicking;
using Content.Server.Afk;
using Content.Shared.Ghost;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Content.Server.Maps;


namespace Content.Server.ADT.Discord.Adminwho;

public sealed class DiscordAdminInfoSenderSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IAdminManager _adminMgr = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private TimeSpan _nextSendTime = TimeSpan.MinValue;
    private readonly TimeSpan _delayInterval = TimeSpan.FromMinutes(1);

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
            if (admin == null)
                return;

            var adminData = _adminMgr.GetAdminData(admin)!;
            DebugTools.AssertNotNull(adminData);

            var afk = IoCManager.Resolve<IAfkManager>();

            if (adminData.Stealth)
                continue;
            sb.Append(admin.Name);
            if (adminData.Title is { } title)
                sb.Append($": [{title}]");

            if (afk.IsAfk(admin))
                sb.Append("[АФК]");

            if (admin.AttachedEntity != null &&
            TryComp<GhostComponent>(admin.AttachedEntity.Value, out var _))
                sb.Append("[Агост]");

            var gameTickerAdmin = _entities.System<GameTicker>();
            if (!gameTickerAdmin.PlayerGameStatuses.TryGetValue(admin.UserId, out var status)
            || status is not PlayerGameStatus.JoinedGame)
                sb.Append("[Лобби]");

            sb.AppendLine();
        }

        if (sb.Length == 0)
            sb.Append("Null админов");

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

        var countPlayer = _playerManager.PlayerCount;
        var countPlayerMax = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var mapName = _gameMapManager.GetSelectedMap();
        var selectGameRule = _gameTicker.CurrentPreset;

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

        embed.Fields.Add(new WebhookEmbedField { Name = "Player", Value = $"{countPlayer}/{countPlayerMax}", Inline = true });
        if (mapName != null)
            embed.Fields.Add(new WebhookEmbedField { Name = "Карта", Value = mapName.MapName, Inline = true });
        if (selectGameRule != null)
            embed.Fields.Add(new WebhookEmbedField { Name = "Режим", Value = Loc.GetString(selectGameRule.ModeTitle), Inline = true });

        var payload = new WebhookPayload
        {
            Embeds = new List<WebhookEmbed> { embed },
            Username = Loc.GetString("username-webhook-adminwho")
        };

        var identifier = webhookData.ToIdentifier();
        await _discord.CreateMessage(identifier, payload);
    }
}
