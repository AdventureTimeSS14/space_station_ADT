﻿using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;
using Content.Server.Discord;
using Content.Server.GameTicking;

namespace Content.Server.ServerUpdates;

/// <summary>
/// Responsible for restarting the server periodically or for update, when not disruptive.
/// </summary>
/// <remarks>
/// This was originally only designed for restarting on *update*,
/// but now also handles periodic restarting to keep server uptime via <see cref="CCVars.ServerUptimeRestartMinutes"/>.
/// </remarks>
public sealed class ServerUpdateManager : IPostInjectInit
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IWatchdogApi _watchdog = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IBaseServer _server = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private ISawmill _sawmill = default!;

    [ViewVariables]
    private bool _updateOnRoundEnd;

    private TimeSpan? _restartTime;

    private TimeSpan _uptimeRestart;

    public void Initialize()
    {
        _watchdog.UpdateReceived += WatchdogOnUpdateReceived;
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;

        _cfg.OnValueChanged(
            CCVars.ServerUptimeRestartMinutes,
            minutes => _uptimeRestart = TimeSpan.FromMinutes(minutes),
            true);
    }

    public void Update()
    {
        if (_restartTime != null)
        {
            if (_restartTime < _gameTiming.RealTime)
            {
                DoShutdown();
            }
        }
        else
        {
            if (ShouldShutdownDueToUptime())
            {
                ServerEmptyUpdateRestartCheck("uptime");
            }
        }
    }

    /// <summary>
    /// Notify that the round just ended, which is a great time to restart if necessary!
    /// </summary>
    /// <returns>True if the server is going to restart.</returns>
    public bool RoundEnded()
    {
        if (_updateOnRoundEnd || ShouldShutdownDueToUptime())
        {
            DoShutdown();
            return true;
        }

        return false;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Connected:
                if (_restartTime != null)
                    _sawmill.Debug("Aborting server restart timer due to player connection");

                _restartTime = null;
                break;
            case SessionStatus.Disconnected:
                ServerEmptyUpdateRestartCheck("last player disconnect");
                break;
        }
    }

    private void WatchdogOnUpdateReceived()
    {
        _chatManager.DispatchServerAnnouncement(Loc.GetString("server-updates-received"));
        _updateOnRoundEnd = true;
        ServerEmptyUpdateRestartCheck("update notification");
        SendDiscordWebHookUpdateMessage(); // ADT-Tweak
    }

    /// <summary>
    ///     Checks whether there are still players on the server,
    /// and if not starts a timer to automatically reboot the server if an update is available.
    /// </summary>
    private void ServerEmptyUpdateRestartCheck(string reason)
    {
        // Can't simple check the current connected player count since that doesn't update
        // before PlayerStatusChanged gets fired.
        // So in the disconnect handler we'd still see a single player otherwise.
        var playersOnline = _playerManager.Sessions.Any(p => p.Status != SessionStatus.Disconnected);
        if (playersOnline || !(_updateOnRoundEnd || ShouldShutdownDueToUptime()))
        {
            // Still somebody online.
            return;
        }

        if (_restartTime != null)
        {
            // Do nothing because we already have a timer running.
            return;
        }

        var restartDelay = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.UpdateRestartDelay));
        _restartTime = restartDelay + _gameTiming.RealTime;

        _sawmill.Debug("Started server-empty restart timer due to {Reason}", reason);
    }

    private void DoShutdown()
    {
        _sawmill.Debug($"Shutting down via {nameof(ServerUpdateManager)}!");
        var reason = _updateOnRoundEnd ? "server-updates-shutdown" : "server-updates-shutdown-uptime";
        _server.Shutdown(Loc.GetString(reason));
    }

    private bool ShouldShutdownDueToUptime()
    {
        return _uptimeRestart != TimeSpan.Zero && _gameTiming.RealTime > _uptimeRestart;
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("restart");
    }
    // ADT-Tweak-start: Отправка сообщения в Discord при обновлении сервера
    public async void SendDiscordWebHookUpdateMessage()
    {
        if (!string.IsNullOrWhiteSpace(_cfg.GetCVar(ADTDiscordWebhookCCVars.DiscordServerUpdateWebhook)))
        {
            var webhookUrl = _cfg.GetCVar(ADTDiscordWebhookCCVars.DiscordServerUpdateWebhook);
            if (webhookUrl == null)
                return;

            if (await _discord.GetWebhook(webhookUrl) is not { } webhookData)
                return;

            // Получение данных сервера
            var serverName = _cfg.GetCVar<string>("game.hostname");
            var serverDesc = _cfg.GetCVar<string>("game.desc");
            var engineVersion = _cfg.GetCVar<string>("build.engine_version");
            var buildVersion = _cfg.GetCVar<string>("build.version");

            // Сообщение о перезапуске сервера
            var descContent = "Обновление получено, сервер автоматически перезапустится для обновления в конце этого раунда.";

            // Определение состояния раунда
            var gameTicker = _entitySystemManager.GetEntitySystem<GameTicker>();
            var roundDescription = gameTicker.RunLevel switch
            {
                GameRunLevel.PreRoundLobby => gameTicker.RoundId == 0
                    ? "pre-round lobby after server restart"
                    : $"pre-round lobby for round {gameTicker.RoundId + 1}",
                GameRunLevel.InRound => $"round {gameTicker.RoundId}",
                GameRunLevel.PostRound => $"post-round {gameTicker.RoundId}",
                _ => throw new ArgumentOutOfRangeException(nameof(gameTicker.RunLevel), $"{gameTicker.RunLevel} was not matched."),
            };

            // Формирование структуры embed
            var embed = new WebhookEmbed
            {
                Title = "Обновление пришло",
                Description = descContent,
                Color = 0x0e9c00,
                Footer = new WebhookEmbedFooter
                {
                    Text = $"{serverName} ({roundDescription})"
                },
                Fields = new List<WebhookEmbedField>()
            };

            // Добавление полей только если они не пустые
            AddIfNotEmpty(embed.Fields, "Название сервера", serverName);
            AddIfNotEmpty(embed.Fields, "Описание сервера", serverDesc);
            AddIfNotEmpty(embed.Fields, "RobustToolbox version", engineVersion);
            AddIfNotEmpty(embed.Fields, "Build version", buildVersion);

            // Формирование полезной нагрузки
            var payload = new WebhookPayload
            {
                Embeds = new List<WebhookEmbed> { embed },
                Username = Loc.GetString("username-webhook-update")
            };

            // Проверка, нужно ли добавлять пинг
            var shouldPingOnUpdate = _cfg.GetCVar(ADTDiscordWebhookCCVars.ShouldPingOnUpdate);
            if (shouldPingOnUpdate)
            {
                // Добавляем пинг в поле Content. Это будет сообщение, которое будет сверху
                payload.Content = "<@&1275740664264659017>"; // ID роли "Обновления"
            }

            // Отправка сообщения в Discord
            var identifier = webhookData.ToIdentifier();
            payload.AllowedMentions.AllowRoleMentions();
            await _discord.CreateMessage(identifier, payload);
        }
    }

    // Вспомогательный метод для добавления полей в embed
    private void AddIfNotEmpty(List<WebhookEmbedField> fields, string fieldName, string? fieldValue)
    {
        if (!string.IsNullOrWhiteSpace(fieldValue))
        {
            fields.Add(new WebhookEmbedField { Name = fieldName, Value = fieldValue, Inline = true });
        }
    }
    // ADT-Tweak-end
}
