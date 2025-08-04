using Robust.Shared.Configuration;
using Content.Shared.Atmos;
using Robust.Shared;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class ADTCCVars
{
    /*
    * Barks
    */
    public static readonly CVarDef<bool> BarksEnabled =
        CVarDef.Create("barks.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMaxPitch =
        CVarDef.Create("barks.max_pitch", 1.5f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMinPitch =
        CVarDef.Create("barks.min_pitch", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMinDelay =
        CVarDef.Create("barks.min_delay", 0.1f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMaxDelay =
        CVarDef.Create("barks.max_delay", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ReplaceTTSWithBarks =
        CVarDef.Create("barks.replace_tts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("barks.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
    * Radial menu
    */
    public static readonly CVarDef<bool> CenterRadialMenu =
        CVarDef.Create("radialmenu.center", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
    * Phantom
    */

    public static readonly CVarDef<int> PhantomMinPlayers =
        CVarDef.Create("phantom.min_players", 25);

    public static readonly CVarDef<int> PhantomMaxDifficulty =
        CVarDef.Create("phantom.max_difficulty", 15);

    public static readonly CVarDef<int> PhantomMaxPicks =
        CVarDef.Create("phantom.max_picks", 10);

    /*
    * Discord
    */

    public static readonly CVarDef<string> DiscordBansWebhook =
        CVarDef.Create("discord.bans_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /*
    * Offer Items
    */
    public static readonly CVarDef<bool> OfferModeIndicatorsPointShow =
        CVarDef.Create("hud.offer_mode_indicators_point_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /*
    * Language
    */
    public static readonly CVarDef<bool> EnableLanguageFonts =
        CVarDef.Create("lang.enable_fonts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    #region Admin

    /// <summary>
    /// Включает или отключает уведомления администраторов о сообщениях,
    /// содержащих оскорбительные выражения в адрес родственников.
    /// </summary>
    public static readonly CVarDef<bool> ChatFilterAdminAlertEnable =
        CVarDef.Create("admin.chat_filter_admina_alert", false, CVar.SERVER | CVar.ARCHIVE);

    #endregion

    /// <summary>
    /// Включает или отключает отображение дополнительной лобби-панели в пользовательском интерфейсе.
    /// При значении true панель отображается, при false - скрывается.
    /// </summary>
    public static readonly CVarDef<bool> ExtraLobbyPanelEnabled =
        CVarDef.Create("ui.show_lobby_panel", true, CVar.REPLICATED | CVar.SERVER);

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

}

