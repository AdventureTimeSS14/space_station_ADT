using Robust.Shared.Configuration;
using Robust.Shared;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class ADTDiscordWebhookCCVars : CVars
{
    /// <summary>
    /// URL of the Discord webhook which will relay adminwho info to the channel.
    /// </summary>
    public static readonly CVarDef<string> DiscordAdminwhoWebhook =
        CVarDef.Create("discord.adminwho_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL | CVar.ARCHIVE);

    /// <summary>
    /// This constant specifies a webhook that will send a message to Discord when a server updates.
    /// </summary>
    public static readonly CVarDef<string> DiscordServerUpdateWebhook =
        CVarDef.Create("discord.server_update_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL | CVar.ARCHIVE);

    /// <summary>
    /// This constant specifies whether a ping should be sent to a specific Discord role
    /// when the server update notification is triggered. If set to <c>true</c>, a ping will be sent to the role.
    /// If set to <c>false</c>, no ping will be sent.
    /// </summary>
    public static readonly CVarDef<bool> ShouldPingOnUpdate =
        CVarDef.Create("discord.server_update_webhook_ping", true, CVar.SERVERONLY | CVar.ARCHIVE);
}
