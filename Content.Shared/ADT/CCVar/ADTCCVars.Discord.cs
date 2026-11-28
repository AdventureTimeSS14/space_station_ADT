using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

public sealed partial class ADTCCVars
{
    public static readonly CVarDef<string> DiscordBansWebhook =
        CVarDef.Create("discord.bans_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Ссылка на канал привязки аккаунта сски к дискорду
    /// </summary>
    public static readonly CVarDef<string> DiscordLinkChannel =
        CVarDef.Create("discord.link_channel", string.Empty, CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Хранит токен бота Discord для авторизации при взаимодействии с Discord API.
    /// Этот токен используется для выполнения операций от имени бота, таких как получение информации о пользователях.
    /// Токен должен быть передан в строковом формате.
    /// </summary>
    public static readonly CVarDef<string> DiscordTokenBot =
        CVarDef.Create("discord.token_bot", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL | CVar.ARCHIVE);

    /// <summary>
    /// Enables or disables authorization via Discord.
    /// </summary>
    public static readonly CVarDef<bool> DiscordAuthEnable =
        CVarDef.Create("discord.auth_enable", false, CVar.SERVERONLY | CVar.ARCHIVE);

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
