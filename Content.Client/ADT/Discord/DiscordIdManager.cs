using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.Discord;
using Robust.Shared.Network;
using Robust.Client.Player; // если нужен

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
    }

    public bool TryGetDiscordId([NotNullWhen(true)] out string? discordId)
    {
        discordId = _discordId;
        return _discordId != null;
    }
}
