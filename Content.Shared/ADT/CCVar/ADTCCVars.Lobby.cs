using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

public sealed partial class ADTCCVars
{
    /// <summary>
    /// Включает или отключает отображение дополнительной лобби-панели в пользовательском интерфейсе.
    /// При значении true панель отображается, при false - скрывается.
    /// </summary>
    public static readonly CVarDef<bool> ExtraLobbyPanelEnabled =
        CVarDef.Create("ui.show_lobby_panel", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> MapRotationEnabled =
        CVarDef.Create("game.map_rotation_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MapRotationExcludeDepth =
        CVarDef.Create("game.map_rotation_exclude_depth", 1, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MapRotationBonusCap =
        CVarDef.Create("game.map_rotation_bonus_cap", 0, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MapRotationMinPlayers =
        CVarDef.Create("game.map_rotation_min_players", 20, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> LobbyWarmupEnabled =
        CVarDef.Create("game.lobby_warmup_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> LobbyWarmupMinPlayers =
        CVarDef.Create("game.lobby_warmup_min_players", 60, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> LobbyWarmupIdle =
        CVarDef.Create("game.lobby_warmup_idle", 30, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> LobbyWarmupMaxWait =
        CVarDef.Create("game.lobby_warmup_max_wait", 180, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Запускать ли голосования за карту и режим при входе в лобби.
    /// </summary>
    public static readonly CVarDef<bool> LobbyAutoVote =
        CVarDef.Create("game.lobby_auto_vote", true, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> AntagRollBonusEnabled =
        CVarDef.Create("antag.roll_bonus_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<float> AntagRollBonusPerRound =
        CVarDef.Create("antag.roll_bonus_per_round", 0.15f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AntagRollBonusInfoRateLimitPeriod =
        CVarDef.Create("antag.roll_bonus_info_rate_limit_period", 5f, CVar.SERVERONLY);

    public static readonly CVarDef<int> AntagRollBonusInfoRateLimitCount =
        CVarDef.Create("antag.roll_bonus_info_rate_limit_count", 3, CVar.SERVERONLY);

    /// <summary>
    /// How many hours pass between full wipes of every stored roll bonus. Zero or less disables wiping.
    /// </summary>
    public static readonly CVarDef<float> AntagRollBonusWipeHours =
        CVarDef.Create("antag.roll_bonus_wipe_hours", 36f, CVar.SERVERONLY);

    public static readonly CVarDef<bool> QueueGamesEnabled =
        CVarDef.Create("queue.games_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<int> QueueGamesScoreToSpin =
        CVarDef.Create("queue.games_score_to_spin", 3, CVar.SERVERONLY);

    public static readonly CVarDef<float> QueueGamesSlotWinChance =
        CVarDef.Create("queue.games_slot_win_chance", 0.05f, CVar.SERVERONLY);

    public static readonly CVarDef<float> QueueGamesSlotBypassMinutes =
        CVarDef.Create("queue.games_slot_bypass_minutes", 10f, CVar.SERVERONLY);

    public static readonly CVarDef<float> QueueGamesTurnTime =
        CVarDef.Create("queue.games_turn_time", 30f, CVar.SERVERONLY);

    public static readonly CVarDef<int> HostReservedSlots =
        CVarDef.Create("queue.host_reserved_slots", 5, CVar.SERVERONLY);
}
