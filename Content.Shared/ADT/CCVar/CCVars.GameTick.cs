using Robust.Shared.Configuration;
using Robust.Shared;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class GameTickCCVars : CVars
{
    /// <summary>
    ///     indicates how much players are required for the round to be considered lowpop.
    ///     Used for dynamic gamemode.
    /// </summary>
    public static readonly CVarDef<float> LowpopThreshold =
        CVarDef.Create("game.players.lowpop_threshold", 20f, CVar.SERVERONLY);
}
