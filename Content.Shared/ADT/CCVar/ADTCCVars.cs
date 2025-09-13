using Robust.Shared.Configuration;
using Content.Shared.ADT.Supermatter;
using Content.Shared.ADT.Supermatter.Components;
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
        CVarDef.Create("barks.replace_tts", false, CVar.CLIENTONLY | CVar.ARCHIVE);

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

    #region Supermatter

    /// <summary>
    ///     Toggles whether or not Cascade delaminations can occur. If disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoCascadeDelam =
        CVarDef.Create("supermatter.do_cascade", true, CVar.SERVER);

    /// <summary>
    ///     The supermatter gains +1 bolts of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerMinPenaltyThreshold =
        CVarDef.Create("supermatter.power_min_penalty_threshold", 3000f, CVar.SERVER);

    /// <summary>
    ///     The cutoff on power properly doing damage, pulling shit around.
    ///     The supermatter will also spawn anomalies, and gains +2 bolts of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerPenaltyThreshold =
        CVarDef.Create("supermatter.power_penalty_threshold", 5000f, CVar.SERVER);

    /// <summary>
    ///     Above this, the supermatter spawns anomalies at an increased rate, and gains +1 bolt of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterSeverePowerPenaltyThreshold =
        CVarDef.Create("supermatter.power_penalty_threshold_severe", 7000f, CVar.SERVER);

    /// <summary>
    ///     Above this, the supermatter spawns pyro anomalies at an increased rate, and gains +1 bolt of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterCriticalPowerPenaltyThreshold =
        CVarDef.Create("supermatter.power_penalty_threshold_critical", 9000f, CVar.SERVER);

    /// <summary>
    ///     The minimum pressure for a pure ammonia atmosphere to begin being consumed.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaConsumptionPressure =
        CVarDef.Create("supermatter.ammonia_consumption_pressure", Atmospherics.OneAtmosphere * 0.01f, CVar.SERVER);

    /// <summary>
    ///     How the amount of ammonia consumed per tick scales with partial pressure.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaPressureScaling =
        CVarDef.Create("supermatter.ammonia_pressure_scaling", Atmospherics.OneAtmosphere * 0.05f, CVar.SERVER);

    /// <summary>
    ///     How much the amount of ammonia consumed per tick scales with the gas mix power ratio.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaGasMixScaling =
        CVarDef.Create("supermatter.ammonia_gas_mix_scaling", 0.3f, CVar.SERVER);

    /// <summary>
    ///     The amount of matter power generated for every mole of ammonia consumed.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaPowerGain =
        CVarDef.Create("supermatter.ammonia_power_gain", 10f, CVar.SERVER);

    /// <summary>
    ///     When true, bypass the normal checks to determine delam type, and instead use the type chosen by supermatter.forced_delam_type
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoForceDelam =
        CVarDef.Create("supermatter.do_force_delam", false, CVar.SERVER);

    /// <summary>
    ///     Maximum safe operational temperature in degrees Celsius.
    ///     Supermatter begins taking damage above this temperature.
    /// </summary>
    public static readonly CVarDef<float> SupermatterHeatPenaltyThreshold =
        CVarDef.Create("supermatter.heat_penalty_threshold", 40f, CVar.SERVER);

    /// <summary>
    ///     The percentage of the supermatter's matter power that is converted into power each atmos tick.
    /// </summary>
    public static readonly CVarDef<float> SupermatterMatterPowerConversion =
        CVarDef.Create("supermatter.matter_power_conversion", 10f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of gas absorbed by the supermatter during the roundstart grace period.
    /// </summary>
    public static readonly CVarDef<float> SupermatterGasEfficiencyGraceModifier =
        CVarDef.Create("supermatter.gas_efficiency_grace_modifier", 2.5f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of damage that the supermatter takes from absorbing hot gas.
    /// </summary>
    public static readonly CVarDef<float> SupermatterMoleHeatPenalty =
        CVarDef.Create("supermatter.mole_heat_penalty", 350f, CVar.SERVER);

    /// <summary>
    ///     Above this threshold the supermatter will delaminate into a singulo and take damage from gas moles.
    ///     Below this threshold, the supermatter can heal damage.
    /// </summary>
    public static readonly CVarDef<float> SupermatterMolePenaltyThreshold =
        CVarDef.Create("supermatter.mole_penalty_threshold", 1800f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of oxygen released during atmospheric reactions.
    /// </summary>
    public static readonly CVarDef<float> SupermatterOxygenReleaseModifier =
        CVarDef.Create("supermatter.oxygen_release_modifier", 325f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of plasma released during atmospheric reactions.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPlasmaReleaseModifier =
        CVarDef.Create("supermatter.plasma_release_modifier", 750f, CVar.SERVER);

    /// <summary>
    ///     Percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerlossInhibitionGasThreshold =
        CVarDef.Create("supermatter.powerloss_inhibition_gas_threshold", 0.2f, CVar.SERVER);

    /// <summary>
    ///     Moles of the gas needed before the charge inertia chain reaction effect starts.
    ///     Scales powerloss inhibition down until this amount of moles is reached.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerlossInhibitionMoleThreshold =
        CVarDef.Create("supermatter.powerloss_inhibition_mole_threshold", 12f, CVar.SERVER);

    /// <summary>
    ///     Bonus powerloss inhibition boost if this amount of moles is reached.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerlossInhibitionMoleBoostThreshold =
        CVarDef.Create("supermatter.powerloss_inhibition_mole_boost_threshold", 500f, CVar.SERVER);

    /// <summary>
    ///     Base amount of radiation that the supermatter emits.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsBase =
        CVarDef.Create("supermatter.rads_base", 4f, CVar.SERVER);

    /// <summary>
    ///     Directly multiplies the amount of rads put out by the supermatter. Be VERY conservative with this.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsModifier =
        CVarDef.Create("supermatter.rads_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Multiplier on the overall power produced during supermatter atmospheric reactions.
    /// </summary>
    public static readonly CVarDef<float> SupermatterReactionPowerModifier =
        CVarDef.Create("supermatter.reaction_power_modifier", 0.55f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount that atmospheric reactions increase the supermatter's temperature.
    /// </summary>
    public static readonly CVarDef<float> SupermatterThermalReleaseModifier =
        CVarDef.Create("supermatter.thermal_release_modifier", 5f, CVar.SERVER);

    /// <summary>
    ///     How often the supermatter should announce its status.
    /// </summary>
    public static readonly CVarDef<float> SupermatterYellTimer =
        CVarDef.Create("supermatter.yell_timer", 60f, CVar.SERVER);

    #endregion

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


    /// <summary>
    /// Кол-во предыдущих карт, которые будут исключены из голосования.
    /// </summary>
    public static readonly CVarDef<int> MapVoteRecentBanDepth =
        CVarDef.Create("game.map_vote_recent_ban_depth", 3, CVar.SERVER | CVar.ARCHIVE);


    public static readonly CVarDef<float> BookPrinterUploadCooldown =
        CVarDef.Create("bookprinter.upload_cooldown", 3600.0f, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BookPrinterUploadCooldownEnabled =
        CVarDef.Create("bookprinter.upload_cooldown_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);
}

