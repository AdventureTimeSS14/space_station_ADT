using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.ADT.Administration;
using Content.Server.Database;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Discord;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Discord;

public sealed class ServerDiscordIdManager : EntitySystem
{
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly Dictionary<NetUserId, string?> _cachedDiscordIds = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("discord-id");

        _net.RegisterNetMessage<MsgDiscordIdInfo>();

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        _net.Disconnect += OnDisconnected;
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        var userId = e.Channel.UserId;
        _cachedDiscordIds.Remove(userId);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            return;

        var session = args.Session;
        var userId = session.UserId;

        if (_cachedDiscordIds.ContainsKey(userId))
        {
            _sawmill.Warning($"Discord ID for {userId} already cached at InGame. Overwriting.");
        }

        var discordId = await LoadDiscordId(userId);
        _cachedDiscordIds[userId] = discordId;

        string? discordUsername = null;

        if (discordId != null && ulong.TryParse(discordId, out var discordUlong))
        {
            try
            {
                var cfg = IoCManager.Resolve<IConfigurationManager>();
                var botToken = cfg.GetCVar(ADTCCVars.DiscordTokenBot);
                discordUsername = await AuthApiHelper.GetAccountDiscord(discordUlong, botToken);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to fetch Discord username for {discordId}: {ex}");
            }
        }

        var msg = new MsgDiscordIdInfo
        {
            UserId = userId,
            DiscordId = discordId,
            DiscordUsername = discordUsername
        };

        _net.ServerSendMessage(msg, session.Channel);
    }

    private async Task<string?> LoadDiscordId(NetUserId userId)
    {
        try
        {
            var discordId = await _db.GetDiscordIdAsync(userId.UserId);
            _sawmill.Debug($"Loaded Discord ID for {userId}: {discordId ?? "null"}");
            return discordId;
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to load Discord ID for {userId}: {ex}");
            return null;
        }
    }

    public bool TryGetDiscordId(NetUserId userId, [NotNullWhen(true)] out string? discordId)
    {
        return _cachedDiscordIds.TryGetValue(userId, out discordId);
    }

    public void InvalidateCache(NetUserId userId)
    {
        _cachedDiscordIds.Remove(userId);
    }

    public void SetDiscordId(NetUserId userId, string? discordId)
    {
        _cachedDiscordIds[userId] = discordId;
    }
}
