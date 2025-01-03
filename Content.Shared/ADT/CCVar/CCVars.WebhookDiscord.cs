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
    /// URL of the Discord adminchat info to the channel.
    /// </summary>
    public static readonly CVarDef<string> DiscordAdminchatWebhook =
        CVarDef.Create("discord.adminchat_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL | CVar.ARCHIVE);
}
