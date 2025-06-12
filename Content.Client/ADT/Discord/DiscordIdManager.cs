using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.Discord;
using Robust.Shared.Network;

namespace Content.Client.ADT.Discord;

public sealed class DiscordIdManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    private string? _discordId;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgDiscordIdInfo>(OnDiscordIdInfo);
    }

    private void OnDiscordIdInfo(MsgDiscordIdInfo msg)
    {
        _discordId = msg.DiscordId;
        _discordUsername = msg.DiscordUsername;
    }

    public bool TryGetDiscordId([NotNullWhen(true)] out string? discordId)
    {
        discordId = _discordId;
        return _discordId != null;
    }

    private string? _discordUsername;

    public bool TryGetDiscordUsername([NotNullWhen(true)] out string? username)
    {
        username = _discordUsername;
        return _discordUsername != null;
    }
}
