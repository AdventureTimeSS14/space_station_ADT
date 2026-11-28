using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

public sealed partial class ADTCCVars
{
    /// <summary>
    /// Whether or not to spawn space whales if the entity is too far away from the station
    /// </summary>
    public static readonly CVarDef<bool> SpaceWhaleSpawn =
        CVarDef.Create("misc.space_whale_spawn", true, CVar.SERVER);

    /// <summary>
    /// The distance to spawn a space whale from the station
    /// </summary>
    public static readonly CVarDef<int> SpaceWhaleSpawnDistance =
        CVarDef.Create("misc.space_whale_spawn_distance", 1965, CVar.SERVER);

    /// <summary>
    /// Whether ghostbar is enabled.
    /// </summary>
    public static readonly CVarDef<bool> GhostbarEnabled =
        CVarDef.Create("ghostbar.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Whether planet spawning is enabled.
    /// </summary>
    public static readonly CVarDef<bool> PlanetSpawnerEnabled =
        CVarDef.Create("planet_spawner.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Whether biome generation on planets is enabled.
    /// </summary>
    public static readonly CVarDef<bool> BiomeGenerationEnabled =
        CVarDef.Create("biome.generation_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Whether overpressure pipe damage is enabled.
    /// When enabled, pipes with OverpressurePipeDamageComponent will take damage when pressure exceeds the limit.
    /// </summary>
    public static readonly CVarDef<bool> OverpressurePipeDamageEnabled =
        CVarDef.Create("atmos.overpressure_pipe_damage_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether blob tiles are allowed to spread onto space tiles.
    /// </summary>
    public static readonly CVarDef<bool> BlobCanGrowInSpace =
        CVarDef.Create("blob.grow_space", true, CVar.SERVER);

    /// <summary>
    /// Should heretic ascension ritual be cancelled if heretic hasn't completed their objectives.
    /// </summary>
    public static readonly CVarDef<bool> HereticAscensionRequiresObjectives =
        CVarDef.Create("heretic.ascension_requires_objectives", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> LogicEnabled =
        CVarDef.Create("adt.logic.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Сколько элементов помещается в одну схему.
    /// </summary>
    public static readonly CVarDef<int> LogicMaxElements =
        CVarDef.Create("adt.logic.max_elements", 64, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Сколько проводов помещается в одну схему.
    /// </summary>
    public static readonly CVarDef<int> LogicMaxWires =
        CVarDef.Create("adt.logic.max_wires", 128, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Сколько схем может тикать одновременно.
    /// </summary>
    public static readonly CVarDef<int> LogicMaxActiveCircuits =
        CVarDef.Create("adt.logic.max_active_circuits", 64, CVar.SERVERONLY);

    /// <summary>
    /// Сколько тиков сервера проходит между обновлениями схемы.
    /// </summary>
    public static readonly CVarDef<int> LogicTickInterval =
        CVarDef.Create("adt.logic.tick_interval", 2, CVar.SERVERONLY);

    /// <summary>
    /// Предел длины текстового сигнала в символах.
    /// </summary>
    public static readonly CVarDef<int> LogicMaxSignalLength =
        CVarDef.Create("adt.logic.max_signal_length", 64, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Предел длины значения в настройках элемента.
    /// </summary>
    public static readonly CVarDef<int> LogicMaxConfigLength =
        CVarDef.Create("adt.logic.max_config_length", 512, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Предел времени в миллисекундах, который все схемы вместе могут занять за один тик.
    public static readonly CVarDef<float> LogicBudgetMs =
        CVarDef.Create("adt.logic.budget_ms", 2f, CVar.SERVERONLY);

    /// <summary>
    ///     When true, Jetpacks can be enabled anywhere, even in gravity.
    /// </summary>
    public static readonly CVarDef<bool> JetpackEnableAnywhere =
        CVarDef.Create("jetpack.enable_anywhere", false, CVar.REPLICATED);

    /// <summary>
    ///     When true, jetpacks can be enabled on grids that have zero gravity.
    /// </summary>
    public static readonly CVarDef<bool> JetpackEnableInNoGravity =
        CVarDef.Create("jetpack.enable_in_no_gravity", true, CVar.REPLICATED);

    /// <summary>
    /// Maximum number of traits that can be selected globally.
    /// </summary>
    public static readonly CVarDef<int> MaxTraitCount =
        CVarDef.Create("ic.traits.max_count", 10, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum trait points available to spend.
    /// Traits with positive cost consume points, negative cost traits grant points.
    /// </summary>
    public static readonly CVarDef<int> MaxTraitPoints =
        CVarDef.Create("ic.traits.max_points", 0, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> XenobiologyBreedingInterval =
        CVarDef.Create("vg.xenobiology.breeding_interval", 2f, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum number of slimes allowed on the same grid before breeding
    ///     becomes increasingly throttled. This is a soft cap driven by a progressive
    ///     slowdown rather than a hard stop. Players are expected to cull the population.
    /// </summary>
    public static readonly CVarDef<int> XenobiologyMaxSlimesPerGrid =
        CVarDef.Create("xenobiology.max_slimes_per_grid", 60, CVar.SERVERONLY);

    /// <summary>
    ///     The number of slimes on a grid at which breeding slowdown begins to take effect.
    ///     Below this value, breeding is unaffected.
    /// </summary>
    public static readonly CVarDef<int> XenobiologyBreedingSlowdownStart =
        CVarDef.Create("xenobiology.breeding_slowdown_start", 30, CVar.SERVERONLY);

    /// <summary>
    ///     How aggressively breeding is throttled once the population exceeds the slowdown start.
    ///     A value of 1.0 means offspring count is reduced linearly with the population,
    ///     hitting zero at the max cap. Lower values make the slowdown gentler.
    /// </summary>
    public static readonly CVarDef<float> XenobiologyBreedingSlowdownFactor =
        CVarDef.Create("xenobiology.breeding_slowdown_factor", 0.6f, CVar.SERVERONLY);
}
