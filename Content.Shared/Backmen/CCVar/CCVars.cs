using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class CCVars
{
    public static readonly CVarDef<bool>
        GameDiseaseEnabled = CVarDef.Create("game.disease", true, CVar.SERVERONLY);
}
