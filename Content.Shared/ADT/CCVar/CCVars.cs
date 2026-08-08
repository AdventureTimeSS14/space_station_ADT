// Simple Station

using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class SimpleStationCCVars
{
    /*
     * Silicons
     */
    #region Silicons
    /// <summary>
    ///     The amount of time between NPC Silicons draining their battery in seconds.
    /// </summary>
    public static readonly CVarDef<float> SiliconNpcUpdateTime =
        CVarDef.Create("silicon.npcupdatetime", 1.5f, CVar.SERVERONLY);
    #endregion Silicons

    /*
    * Jetpack
    */
    #region Jetpack System

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

    #endregion

    /*
     * Traits
     */

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
        CVarDef.Create("vg.xenobiology.breeding_interval", 1f, CVar.SERVERONLY);

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
