using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Players;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.ADT.CCVar;
using Content.Server.Discord;
using Content.Server.ADT.Administration;
using System.Linq;

namespace Content.Server.GameTicking
{
    [UsedImplicitly]
    public sealed partial class GameTicker
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private void InitializePlayer()
        {
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private async void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            if (_mind.TryGetMind(session.UserId, out var mindId, out var mind))
            {
                if (args.NewStatus != SessionStatus.Disconnected)
                {
                    _pvsOverride.AddSessionOverride(mindId.Value, session);
                }
            }

            DebugTools.Assert(session.GetMind() == mindId);

            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                {
                    AddPlayerToDb(args.Session.UserId.UserId);

                    // Always make sure the client has player data.
                    if (session.Data.ContentDataUncast == null)
                    {
                        var data = new ContentPlayerData(session.UserId, args.Session.Name);
                        data.Mind = mindId;
                        session.Data.ContentDataUncast = data;
                    }

                    // Make the player actually join the game.
                    // timer time must be > tick length
                    // Timer.Spawn(0, args.Session.JoinGame); // Corvax-Queue: Moved to `JoinQueueManager`

                    var record = await _db.GetPlayerRecordByUserId(args.Session.UserId);
                    var firstConnection = record != null &&
                                          Math.Abs((record.FirstSeenTime - record.LastSeenTime).TotalMinutes) < 600; //пока у игрока не будет наиграно 10ч, будет высвечиваться надпись что он новичок, для облегчении слежки модерации, ADT

                    var firstSeenTime = record?.FirstSeenTime.ToString("dd.MM.yyyy") ?? "неизвестно"; // дата первого подключения, ADT

                    // ADT-Tweak-start
                    if (firstConnection)
                    {
                        var creationDate = "Не удалось получить дату создания";
                        try
                        {
                            // Получаем дату создания аккаунта через API визардов
                            creationDate = await AuthApiHelper.GetCreationDate(args.Session.UserId.ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Ошибка при получении даты создания аккаунта: {ex.Message}");
                        }

                        _chatManager.SendAdminAnnouncementColor(
                            "\nВНИМАНИЕ!!!\n" +
                            $"Зашёл НОВИЧОК {args.Session.Name} первый заход: {firstSeenTime}.\n" +
                            $"Дата создания аккаунта: {creationDate}\n" +
                            "Администрации быть внимательней :0, у данного игрока меньше 10ч на нашем сервере.\n" +
                            "ВНИМАНИЕ!!!",
                            colorOverrid: Color.White
                        );

                        // Получаем всех администраторов
                        var clients = _adminManager.ActiveAdmins
                        .Where(admin => _adminManager.GetAdminData(admin)?.Flags.HasFlag(AdminFlags.Adminchat) == true)
                        .Select(p => p.Channel).ToList();

                        Filter filter = Filter.Empty();
                        foreach (var client in clients)
                        {
                            var sessionAdmin = _playerManager.GetSessionByChannel(client);
                            filter.AddPlayer(sessionAdmin);
                        }

                        var soundPath = new ResPath("/Audio/ADT/Misc/sgu.ogg");
                        var audioParams = AudioParams.Default.WithVolume(-8f);
                        var replay = false;

                        // Каждому воспроизводим звук
                        _audio.PlayGlobal(
                            soundPath,
                            filter,
                            replay,
                            audioParams
                        );
                    }
                    else
                    {
                        _chatManager.SendAdminAnnouncement(Loc.GetString("player-join-message", ("name", args.Session.Name)));
                    }
                    // ADT-Tweak-end

                    // ADT-Tweak-start: Постит в дис админчата, о заходе новых игроков
                    if (!string.IsNullOrEmpty(_cfg.GetCVar(ADTDiscordWebhookCCVars.DiscordAdminchatWebhook)) && firstConnection)
                    {
                        var webhookUrl = _cfg.GetCVar(ADTDiscordWebhookCCVars.DiscordAdminchatWebhook);

                        if (webhookUrl == null)
                            return;

                        if (await _discord.GetWebhook(webhookUrl) is not { } webhookData)
                            return;
                        var payload = new WebhookPayload
                        {
                            Content = $"**Оповещение: ЗАШЁЛ НОВИЧОК** ({args.Session.Name})"
                        };
                        var identifier = webhookData.ToIdentifier();
                        await _discord.CreateMessage(identifier, payload);
                    }
                    // ADT-Tweak-end
                    RaiseNetworkEvent(GetConnectionStatusMsg(), session.Channel);

                    if (firstConnection && _cfg.GetCVar(CCVars.AdminNewPlayerJoinSound))
                        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg"),
                            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false,
                            audioParams: new AudioParams { Volume = -5f });

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTime = _gameTiming.CurTime + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    _userDb.ClientConnected(session);

                    if (mind == null)
                    {
                        if (LobbyEnabled)
                            PlayerJoinLobby(session);
                        else
                            SpawnWaitDb();

                        break;
                    }

                    if (mind.CurrentEntity == null || Deleted(mind.CurrentEntity))
                    {
                        DebugTools.Assert(mind.CurrentEntity == null, "a mind's current entity was deleted without updating the mind");

                        // This player is joining the game with an existing mind, but the mind has no entity.
                        // Their entity was probably deleted sometime while they were disconnected, or they were an observer.
                        // Instead of allowing them to spawn in, we will dump and their existing mind in an observer ghost.
                        SpawnObserverWaitDb();
                    }
                    else
                    {
                        if (_playerManager.SetAttachedEntity(session, mind.CurrentEntity))
                        {
                            PlayerJoinGame(session);
                        }
                        else
                        {
                            Log.Error(
                                $"Failed to attach player {session} with mind {ToPrettyString(mindId)} to its current entity {ToPrettyString(mind.CurrentEntity)}");
                            SpawnObserverWaitDb();
                        }
                    }

                    break;
                }

                case SessionStatus.Disconnected:
                {
                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-leave-message", ("name", args.Session.Name)));
                    if (mindId != null)
                    {
                        _pvsOverride.RemoveSessionOverride(mindId.Value, session);
                    }

                    if (_playerGameStatuses.ContainsKey(args.Session.UserId)) // Corvax-Queue: Delete data only if player was in game
                        _userDb.ClientDisconnected(session);
                    break;
                }
            }
            //When the status of a player changes, update the server info text
            UpdateInfoText();

            async void SpawnWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                SpawnPlayer(session, EntityUid.Invalid);
            }

            async void SpawnObserverWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                JoinAsObserver(session);
            }

            async void AddPlayerToDb(Guid id)
            {
                if (RoundId != 0 && _runLevel != GameRunLevel.PreRoundLobby)
                {
                    await _db.AddRoundPlayers(RoundId, id);
                }
            }
        }

        public HumanoidCharacterProfile GetPlayerProfile(ICommonSession p)
        {
            return (HumanoidCharacterProfile)_prefsManager.GetPreferences(p.UserId).SelectedCharacter;
        }

        public void PlayerJoinGame(ICommonSession session, bool silent = false)
        {
            if (!silent)
                _chatManager.DispatchServerMessage(session, Loc.GetString("game-ticker-player-join-game-message"));

            _playerGameStatuses[session.UserId] = PlayerGameStatus.JoinedGame;
            _db.AddRoundPlayers(RoundId, session.UserId);

            if (_adminManager.HasAdminFlag(session, AdminFlags.Admin))
            {
                if (_allPreviousGameRules.Count > 0)
                {
                    var rulesMessage = GetGameRulesListMessage(true);
                    _chatManager.SendAdminAnnouncementMessage(session, Loc.GetString("starting-rule-selected-preset", ("preset", rulesMessage)));
                }
            }

            RaiseNetworkEvent(new TickerJoinGameEvent(), session.Channel);
        }

        private void PlayerJoinLobby(ICommonSession session)
        {
            _playerGameStatuses[session.UserId] = LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
            _db.AddRoundPlayers(RoundId, session.UserId);

            var client = session.Channel;
            RaiseNetworkEvent(new TickerJoinLobbyEvent(), client);
            RaiseNetworkEvent(GetStatusMsg(session), client);
            RaiseNetworkEvent(GetInfoMsg(), client);
            RaiseLocalEvent(new PlayerJoinedLobbyEvent(session));
        }

        private void ReqWindowAttentionAll()
        {
            RaiseNetworkEvent(new RequestWindowAttentionEvent());
        }
    }

    public sealed class PlayerJoinedLobbyEvent : EntityEventArgs
    {
        public readonly ICommonSession PlayerSession;

        public PlayerJoinedLobbyEvent(ICommonSession playerSession)
        {
            PlayerSession = playerSession;
        }
    }
}
