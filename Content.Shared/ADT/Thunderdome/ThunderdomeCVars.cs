using Robust.Shared.Configuration;

namespace Content.Shared.ADT.Thunderdome;

[CVarDefs]
public sealed partial class ThunderdomeCVars
{
    public static readonly CVarDef<bool> ThunderdomeEnabled =
        CVarDef.Create("thunderdome.enabled", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> ThunderdomeRefill =
        CVarDef.Create("thunderdome.refill", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Master switch for the persistent leaderboard. When off, nothing is read from or written to the database.
    /// </summary>
    public static readonly CVarDef<bool> StatsEnabled =
        CVarDef.Create("thunderdome.stats_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// How often accumulated stats are flushed to the database, in seconds. Only dirty rows are written,
    /// and the whole batch goes out as a single command, so this is cheap even on a busy arena.
    /// </summary>
    public static readonly CVarDef<float> StatsFlushInterval =
        CVarDef.Create("thunderdome.stats_flush_interval", 300f, CVar.SERVERONLY);

    /// <summary>
    /// How long a fetched leaderboard is reused before hitting the database again, in seconds.
    /// </summary>
    public static readonly CVarDef<float> StatsCacheTtl =
        CVarDef.Create("thunderdome.stats_cache_ttl", 30f, CVar.SERVERONLY);

    /// <summary>
    /// Minimum delay between leaderboard requests from a single player, in seconds.
    /// </summary>
    public static readonly CVarDef<float> StatsRequestCooldown =
        CVarDef.Create("thunderdome.stats_request_cooldown", 3f, CVar.SERVERONLY);

    /// <summary>
    /// How many players the leaderboard shows.
    /// </summary>
    public static readonly CVarDef<int> StatsLeaderboardSize =
        CVarDef.Create("thunderdome.stats_leaderboard_size", 10, CVar.SERVERONLY);

    /// <summary>
    /// How long after taking damage a player still counts as "killed by" their attacker, in seconds.
    /// Ghosting, suiciding, leaving or disconnecting inside this window credits the attacker, so bailing
    /// out mid-fight cannot deny a frag. Outside it, nobody is credited, so ghosting in circles cannot farm one.
    /// </summary>
    public static readonly CVarDef<float> StatsCreditWindow =
        CVarDef.Create("thunderdome.stats_credit_window", 30f, CVar.SERVERONLY);

    /// <summary>
    /// A victim that lived shorter than this many seconds awards nothing, to stop players from
    /// repeatedly running into a friend's gun straight off the spawn.
    /// </summary>
    public static readonly CVarDef<float> StatsMinLifetime =
        CVarDef.Create("thunderdome.stats_min_lifetime", 10f, CVar.SERVERONLY);

    /// <summary>
    /// Kills only count towards the persistent leaderboard when at least this many players are in the arena.
    /// </summary>
    public static readonly CVarDef<int> StatsMinPlayers =
        CVarDef.Create("thunderdome.stats_min_players", 2, CVar.SERVERONLY);

    /// <summary>
    /// How many kills of the same victim in one round are worth full value before diminishing returns start.
    /// </summary>
    public static readonly CVarDef<int> StatsRepeatFree =
        CVarDef.Create("thunderdome.stats_repeat_free", 1, CVar.SERVERONLY);

    /// <summary>
    /// Score multiplier applied per repeated kill of the same victim beyond the free ones.
    /// With the default 0.5 a pair farming each other gets 1, 0.5, 0.25, 0.125 and then nothing.
    /// </summary>
    public static readonly CVarDef<float> StatsRepeatDecay =
        CVarDef.Create("thunderdome.stats_repeat_decay", 0.5f, CVar.SERVERONLY);

    /// <summary>
    /// Kills worth less than this are discarded entirely rather than awarding a sliver of score.
    /// </summary>
    public static readonly CVarDef<float> StatsMinWeight =
        CVarDef.Create("thunderdome.stats_min_weight", 0.1f, CVar.SERVERONLY);
}
