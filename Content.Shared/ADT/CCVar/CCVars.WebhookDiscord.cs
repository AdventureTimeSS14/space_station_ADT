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
        CVarDef.Create("discord.adminwho_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// This constant specifies a webhook that will send a message to Discord when a server updates.
    /// </summary>
    public static readonly CVarDef<string> DiscordServerUpdateWebhook =
        CVarDef.Create("discord.server_update_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
