using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

/// <summary>
/// Contains all the CVars used by content.
/// </summary>
/// <remarks>
/// NOTICE FOR FORKS: Put your own CVars in a separate file with a different [CVarDefs] attribute. RT will automatically pick up on it.
/// </remarks>
[CVarDefs]
public sealed partial class CCVars : CVars
{
    // Only debug stuff lives here.

    /// <summary>
    /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
    /// </summary>
    public static readonly CVarDef<bool> DebugOptionVisualizerTest =
        CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

    /// <summary>
    /// Set to true to disable parallel processing in the pow3r solver.
    /// </summary>
    public static readonly CVarDef<bool> DebugPow3rDisableParallel =
        CVarDef.Create("debug.pow3r_disable_parallel", true, CVar.SERVERONLY);

    // ADT Tweak start
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
    // ADT Tweak End
}
