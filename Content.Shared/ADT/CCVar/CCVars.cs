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
}
