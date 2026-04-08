using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
<<<<<<< HEAD
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
=======
>>>>>>> upstreamwiz/master
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
<<<<<<< HEAD
    public const string PrefixAntag = "Antag:";
    public const string PrefixJob = "Job:";
=======
    public const string DbTypeAntag = "Antag";
    public const string DbTypeJob = "Job";
>>>>>>> upstreamwiz/master

    private readonly Dictionary<ICommonSession, List<BanDef>> _cachedRoleBans = new();
    // Cached ban exemption flags are used to handle
    private readonly Dictionary<ICommonSession, ServerBanExemptFlags> _cachedBanExemptions = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgRoleBans>();

        _db.SubscribeToJsonNotification<BanNotificationData>(
            _taskManager,
            _sawmill,
            BanNotificationChannel,
            ProcessBanNotification,
            OnDatabaseNotificationEarlyFilter);

        _userDbData.AddOnLoadPlayer(CachePlayerData);
        _userDbData.AddOnPlayerDisconnect(ClearPlayerData);
    }

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var flags = await _db.GetBanExemption(player.UserId, cancel);

        var netChannel = player.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        var modernHwids = netChannel.UserData.ModernHWIds;
        var roleBans = await _db.GetBansAsync(
            netChannel.RemoteEndPoint.Address,
            player.UserId,
            hwId,
            modernHwids,
            false,
            type: BanType.Role);

        var userRoleBans = new List<BanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        cancel.ThrowIfCancellationRequested();
        _cachedBanExemptions[player] = flags;
        _cachedRoleBans[player] = userRoleBans;

        SendRoleBans(player);
    }

    private void ClearPlayerData(ICommonSession player)
    {
        _cachedBanExemptions.Remove(player);
    }

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new ValueList<ICommonSession>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (player.Status == SessionStatus.Disconnected)
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
    }

    #region Server Bans
    public async void CreateServerBan(CreateServerBanInfo banInfo)
    {
        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Server, null);

        await _db.AddBanAsync(banDef);

        if (_cfg.GetCVar(CCVars.ServerBanResetLastReadRules))
        {
            // Reset their last read rules. They probably need a refresher!
            foreach (var (userId, _) in banInfo.Users)
            {
                await _db.SetLastReadRules(userId, null);
            }
        }

        var adminName = banInfo.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banInfo.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));

        var addressRangeString = banInfo.AddressRanges.Count != 0
            ? "null"
            : string.Join(", ", banInfo.AddressRanges.Select(a => $"{a.Address}/{a.Mask}"));

        var hwidString = banInfo.HWIds.Count == 0
            ? "null"
            : string.Join(", ", banInfo.HWIds);

        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", banDef.Severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", banInfo.Reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        KickMatchingConnectedPlayers(banDef, "newly placed ban");
    }

    private NoteSeverity GetSeverityForServerBan(CreateBanInfo banInfo, CVarDef<string> defaultCVar)
    {
        if (banInfo.Severity != null)
            return banInfo.Severity.Value;

        if (Enum.TryParse(_cfg.GetCVar(defaultCVar), true, out NoteSeverity parsedSeverity))
            return parsedSeverity;

        _sawmill.Error($"CVar {defaultCVar.Name} has invalid ban severity!");
        return NoteSeverity.None;
    }

    private void KickMatchingConnectedPlayers(BanDef def, string source)
    {
        foreach (var player in _playerManager.Sessions)
        {
            if (BanMatchesPlayer(player, def))
            {
                KickForBanDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private bool BanMatchesPlayer(ICommonSession player, BanDef ban)
    {
        var playerInfo = new BanMatcher.PlayerInfo
        {
            UserId = player.UserId,
            Address = player.Channel.RemoteEndPoint.Address,
            HWId = player.Channel.UserData.HWId,
            ModernHWIds = player.Channel.UserData.ModernHWIds,
            // It's possible for the player to not have cached data loading yet due to coincidental timing.
            // If this is the case, we assume they have all flags to avoid false-positives.
            ExemptFlags = _cachedBanExemptions.GetValueOrDefault(player, ServerBanExemptFlags.All),
            IsNewPlayer = false,
        };

        return BanMatcher.BanMatches(ban, playerInfo);
    }

    private void KickForBanDef(ICommonSession player, BanDef def)
    {
        var message = def.FormatBanMessage(_cfg, _localizationManager);
        player.Channel.Disconnect(message);
    }

    #endregion

    #region Role Bans

<<<<<<< HEAD
    // If you are trying to remove timeOfBan, please don't. It's there because the note system groups role bans by time, reason and banning admin.
    // Removing it will clutter the note list. Please also make sure that department bans are applied to roles with the same DateTimeOffset.
    public async void CreateRoleBan<T>(
        NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        ProtoId<T> role,
        uint? minutes,
        NoteSeverity severity,
        string reason,
        DateTimeOffset timeOfBan
    ) where T : class, IPrototype
    {
        string encodedRole;

        // TODO: Note that it's possible to clash IDs here between a job and an antag. The refactor that introduced
        // this check has consciously avoided refactoring Job and Antag prototype.
        // Refactor Job- and Antag- Prototype to introduce a common RolePrototype, which will fix this possible clash.

        //TODO remove this check as part of the above refactor
        if (_prototypeManager.HasIndex<JobPrototype>(role) && _prototypeManager.HasIndex<AntagPrototype>(role))
        {
            _sawmill.Error($"Creating role ban for {role}: cannot create role ban, role is both JobPrototype and AntagPrototype.");

            return;
        }

        // Don't trust the input: make sure the job or antag actually exists.
        if (_prototypeManager.HasIndex<JobPrototype>(role))
            encodedRole = PrefixJob + role;
        else if (_prototypeManager.HasIndex<AntagPrototype>(role))
            encodedRole = PrefixAntag + role;
        else
        {
            _sawmill.Error($"Creating role ban for {role}: cannot create role ban, role is not a JobPrototype or an AntagPrototype.");

            return;
        }

        DateTimeOffset? expires = null;

        if (minutes > 0)
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerRoleBanDef(
=======
    public async void CreateRoleBan(CreateRoleBanInfo banInfo)
    {
        ImmutableArray<BanRoleDef> roleDefs =
        [
            .. ToBanRoleDef(banInfo.JobPrototypes),
            .. ToBanRoleDef(banInfo.AntagPrototypes),
        ];

        if (roleDefs.Length == 0)
            throw new ArgumentException("Must specify at least one role to ban!");

        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Role, roleDefs);

        await AddRoleBan(banDef);

        var length = expires == null
            ? Loc.GetString("cmd-roleban-inf")
            : Loc.GetString("cmd-roleban-until", ("expires", expires));

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));

        _chat.SendAdminAlert(Loc.GetString(
            "cmd-roleban-success",
            ("target", targetName),
            ("role", string.Join(", ", roleDefs)),
            ("reason", banInfo.Reason),
            ("length", length)));

        foreach (var (userId, _) in banInfo.Users)
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
                SendRoleBans(session);
        }
    }

    private async Task<(BanDef Ban, DateTimeOffset? Expires)> CreateBanDef(
        CreateBanInfo banInfo,
        BanType type,
        ImmutableArray<BanRoleDef>? roleBans)
    {
        if (banInfo.Users.Count == 0 && banInfo.HWIds.Count == 0 && banInfo.AddressRanges.Count == 0)
            throw new ArgumentException("Must specify at least one user, HWID, or address range");

        DateTimeOffset? expires = null;
        if (banInfo.Duration is { } duration)
            expires = DateTimeOffset.Now + duration;

        ImmutableArray<int> roundIds;
        if (banInfo.RoundIds.Count > 0)
        {
            roundIds = [..banInfo.RoundIds];
        }
        else if (_systems.TryGetEntitySystem<GameTicker>(out var ticker) && ticker.RoundId != 0)
        {
            roundIds = [ticker.RoundId];
        }
        else
        {
            roundIds = [];
        }

        return (new BanDef(
>>>>>>> upstreamwiz/master
            null,
            type,
            [..banInfo.Users.Select(u => u.UserId)],
            [..banInfo.AddressRanges],
            [..banInfo.HWIds],
            DateTimeOffset.Now,
            expires,
            roundIds,
            await GetPlayTime(banInfo),
            banInfo.Reason,
            GetSeverityForServerBan(banInfo, CCVars.ServerBanDefaultSeverity),
            banInfo.BanningAdmin,
            null,
<<<<<<< HEAD
            encodedRole);
=======
            roles: roleBans), expires);
    }
>>>>>>> upstreamwiz/master

    private async Task<TimeSpan> GetPlayTime(CreateBanInfo banInfo)
    {
        var firstPlayer = banInfo.Users.FirstOrNull()?.UserId;
        if (firstPlayer == null)
            return TimeSpan.Zero;

        return (await _db.GetPlayTimes(firstPlayer.Value))
            .Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)
            ?.TimeSpent ?? TimeSpan.Zero;
    }

    private IEnumerable<BanRoleDef> ToBanRoleDef<T>(IEnumerable<ProtoId<T>> protoIds) where T : class, IPrototype
    {
        return protoIds.Select(protoId =>
        {
<<<<<<< HEAD
            _chat.SendAdminAlert(Loc.GetString("cmd-roleban-existing", ("target", targetUsername ?? "null"), ("role", role)));

            return;
        }
=======
            // TODO: I have no idea if this check is necessary. The previous code was a complete mess,
            // so out of safety I'm leaving this in.
            if (_prototypeManager.HasIndex<JobPrototype>(protoId) && _prototypeManager.HasIndex<AntagPrototype>(protoId))
            {
                throw new InvalidOperationException(
                    $"Creating role ban for {protoId}: cannot create role ban, role is both JobPrototype and AntagPrototype.");
            }
>>>>>>> upstreamwiz/master

            // Don't trust the input: make sure the role actually exists.
            if (!_prototypeManager.HasIndex(protoId))
                throw new UnknownPrototypeException(protoId, typeof(T));

<<<<<<< HEAD
        if (target is not null && _playerManager.TryGetSessionById(target.Value, out var session))
            SendRoleBans(session);
    }

    private async Task<bool> AddRoleBan(ServerRoleBanDef banDef)
    {
        banDef = await _db.AddServerRoleBanAsync(banDef);

        if (banDef.UserId != null
            && _playerManager.TryGetSessionById(banDef.UserId, out var player)
            && _cachedRoleBans.TryGetValue(player, out var cachedBans))
        {
            cachedBans.Add(banDef);
=======
            return new BanRoleDef(PrototypeKindToDbType<T>(), protoId);
        });
    }

    private static string PrototypeKindToDbType<T>() where T : class, IPrototype
    {
        if (typeof(T) == typeof(JobPrototype))
            return DbTypeJob;

        if (typeof(T) == typeof(AntagPrototype))
            return DbTypeAntag;

        throw new ArgumentException($"Unknown prototype kind for role bans: {typeof(T)}");
    }

    private async Task AddRoleBan(BanDef banDef)
    {
        banDef = await _db.AddBanAsync(banDef);

        foreach (var user in banDef.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var player)
                && _cachedRoleBans.TryGetValue(player, out var cachedBans))
            {
                cachedBans.Add(banDef);
            }
>>>>>>> upstreamwiz/master
        }

        return true; //ADT Tweak
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Type != BanType.Role)
            throw new InvalidOperationException("Ban was not a role ban!");

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddUnbanAsync(new UnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        foreach (var user in ban.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var session)
                && _cachedRoleBans.TryGetValue(session, out var roleBans))
            {
                roleBans.RemoveAll(roleBan => roleBan.Id == ban.Id);
                SendRoleBans(session);
            }

        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId)
    {
<<<<<<< HEAD
        return GetRoleBans<JobPrototype>(playerUserId, PrefixJob);
=======
        return GetRoleBans<JobPrototype>(playerUserId);
>>>>>>> upstreamwiz/master
    }

    public HashSet<ProtoId<AntagPrototype>>? GetAntagBans(NetUserId playerUserId)
    {
<<<<<<< HEAD
        return GetRoleBans<AntagPrototype>(playerUserId, PrefixAntag);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(NetUserId playerUserId, string prefix) where T : class, IPrototype
=======
        return GetRoleBans<AntagPrototype>(playerUserId);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(NetUserId playerUserId) where T : class, IPrototype
>>>>>>> upstreamwiz/master
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

<<<<<<< HEAD
        return GetRoleBans<T>(session, prefix);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(ICommonSession playerSession, string prefix) where T : class, IPrototype
=======
        return GetRoleBans<T>(session);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(ICommonSession playerSession) where T : class, IPrototype
>>>>>>> upstreamwiz/master
    {
        if (!_cachedRoleBans.TryGetValue(playerSession, out var roleBans))
            return null;

        var dbType = PrototypeKindToDbType<T>();

        return roleBans
<<<<<<< HEAD
            .Where(ban => ban.Role.StartsWith(prefix, StringComparison.Ordinal))
            .Select(ban => new ProtoId<T>(ban.Role[prefix.Length..]))
            .ToHashSet();
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
=======
            .SelectMany(ban => ban.Roles!.Value)
            .Where(role => role.RoleType == dbType)
            .Select(role => new ProtoId<T>(role.RoleId))
            .ToHashSet();
    }

    public HashSet<BanRoleDef>? GetRoleBans(NetUserId playerUserId)
>>>>>>> upstreamwiz/master
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return _cachedRoleBans.TryGetValue(session, out var roleBans)
<<<<<<< HEAD
            ? roleBans.Select(banDef => banDef.Role).ToHashSet()
=======
            ? roleBans.SelectMany(banDef => banDef.Roles ?? []).ToHashSet()
>>>>>>> upstreamwiz/master
            : null;
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<JobPrototype>> jobs)
    {
<<<<<<< HEAD
        return IsRoleBanned(player, jobs, PrefixJob);
=======
        return IsRoleBanned<JobPrototype>(player, jobs);
>>>>>>> upstreamwiz/master
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<AntagPrototype>> antags)
    {
<<<<<<< HEAD
        return IsRoleBanned(player, antags, PrefixAntag);
    }

    private bool IsRoleBanned<T>(ICommonSession player, List<ProtoId<T>> roles, string prefix) where T : class, IPrototype
=======
        return IsRoleBanned<AntagPrototype>(player, antags);
    }

    private bool IsRoleBanned<T>(ICommonSession player, List<ProtoId<T>> roles) where T : class, IPrototype
>>>>>>> upstreamwiz/master
    {
        var bans = GetRoleBans(player.UserId);

        if (bans is null || bans.Count == 0)
            return false;

<<<<<<< HEAD
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var role in roles)
        {
            if (bans.Contains(prefix + role))
=======
        var dbType = PrototypeKindToDbType<T>();

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var role in roles)
        {
            if (bans.Contains(new BanRoleDef(dbType, role)))
>>>>>>> upstreamwiz/master
                return true;
        }

        return false;
    }

    public void SendRoleBans(ICommonSession pSession)
    {
<<<<<<< HEAD
        var jobBans = GetRoleBans<JobPrototype>(pSession, PrefixJob);
        var jobBansList = new List<string>(jobBans?.Count ?? 0);

        if (jobBans is not null)
        {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var encodedId in jobBans)
            {
                jobBansList.Add(encodedId.ToString().Replace(PrefixJob, ""));
            }
        }

        var antagBans = GetRoleBans<AntagPrototype>(pSession, PrefixAntag);
        var antagBansList = new List<string>(antagBans?.Count ?? 0);

        if (antagBans is not null)
        {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var encodedId in antagBans)
            {
                antagBansList.Add(encodedId.ToString().Replace(PrefixAntag, ""));
            }
        }

        var bans = new MsgRoleBans()
        {
            JobBans = jobBansList,
            AntagBans = antagBansList,
=======
        var bans = new MsgRoleBans()
        {
            JobBans = (GetRoleBans<JobPrototype>(pSession) ?? []).ToList(),
            AntagBans = (GetRoleBans<AntagPrototype>(pSession) ?? []).ToList(),
>>>>>>> upstreamwiz/master
        };

        _sawmill.Debug($"Sent role bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    #endregion

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
