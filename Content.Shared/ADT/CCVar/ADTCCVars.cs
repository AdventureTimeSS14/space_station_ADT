using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed partial class ADTCCVars
{
    /// <summary>
    /// Включает скрытую информацию персонажа (видна только владельцу, антагам и призракам).
    /// </summary>
    public static readonly CVarDef<bool> ExploitableSecrets =
        CVarDef.Create("ic.exploitable_info", true, CVar.SERVER | CVar.REPLICATED);

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
        CVarDef.Create("barks.replace_tts", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("barks.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BossMusicEnabled =
        CVarDef.Create("adt.boss_music_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BossMusicVolume =
        CVarDef.Create("adt.boss_music_volume", 1.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CenterRadialMenu =
        CVarDef.Create("radialmenu.center", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> EnableTextOutline =
        CVarDef.Create("accessibility.text_outline", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> OfferModeIndicatorsPointShow =
        CVarDef.Create("hud.offer_mode_indicators_point_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> EnableLanguageFonts =
        CVarDef.Create("lang.enable_fonts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If enabled, job icons in chat and status icons are available.
    /// When disabled on server, icons will not be shown for anyone.
    /// Players can also disable icons in their client settings.
    /// </summary>
    public static readonly CVarDef<bool> EnableJobIconAnimation =
        CVarDef.Create("adt.job_icon_animation_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Client-side setting to toggle job icons in chat.
    /// Icons are only shown if both server and client allow them.
    /// </summary>
    public static readonly CVarDef<bool> EnableChatJobIcons =
        CVarDef.Create("adt.chat_job_icons_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);


    public static readonly CVarDef<string> HeadshotDomain =
        CVarDef.Create("ic.headshot_domain", "i.pinimg.com", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum size of headshot images in bytes (default: 5MB).
    /// </summary>
    public static readonly CVarDef<int> HeadshotMaxSize =
        CVarDef.Create("ic.headshot_max_size", 5 * 1024 * 1024, CVar.SERVER);

    /// <summary>
    /// Headshot cache duration in minutes (default: 30 minutes).
    /// </summary>
    public static readonly CVarDef<float> HeadshotCacheDuration =
        CVarDef.Create("ic.headshot_cache_duration", 30f, CVar.SERVER);

    /// <summary>
    /// Maximum number of headshot images in cache (default: 100).
    /// </summary>
    public static readonly CVarDef<int> HeadshotMaxCacheCount =
        CVarDef.Create("ic.headshot_max_cache_count", 100, CVar.SERVER);

    /// <summary>
    /// If enabled, pointing messages are duplicated into chat with an entity icon.
    /// When disabled, only popups are shown
    /// </summary>
    public static readonly CVarDef<bool> PointingChatIconsEnabled =
        CVarDef.Create("adt.pointing_chat_icons_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Client-side setting to toggle pointing messages in chat.
    /// Replicated to server so it can skip sending chat messages to players who disabled it.
    /// </summary>
    public static readonly CVarDef<bool> EnableChatPointingIcons =
        CVarDef.Create("adt.chat_pointing_icons_enabled", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENT);

    public static readonly CVarDef<bool> ChatHighlightSoundEnabled =
        CVarDef.Create("adt.chat_highlight_sound_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<string> ChatHighlightSoundPath =
        CVarDef.Create("adt.chat_highlight_sound_path", "/Audio/ADT/UI/ChatHighlight/notification1.ogg", CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<float> ChatHighlightSoundVolume =
        CVarDef.Create("adt.chat_highlight_sound_volume", 1f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Включает или отключает уведомления администраторов о сообщениях,
    /// содержащих оскорбительные выражения в адрес родственников.
    /// </summary>
    public static readonly CVarDef<bool> ChatFilterAdminAlertEnable =
        CVarDef.Create("admin.chat_filter_admina_alert", false, CVar.SERVER | CVar.ARCHIVE);
}
