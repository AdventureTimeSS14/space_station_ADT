using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

[CVarDefs]
public sealed class RMCCVars : CVars
{
    public static readonly CVarDef<bool> RMCActiveInputMoverEnabled =
        CVarDef.Create("rmc.active_input_mover_enabled", true, CVar.REPLICATED | CVar.SERVER);
}
