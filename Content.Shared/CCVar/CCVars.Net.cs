using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<float> NetAtmosDebugOverlayTickRate =
        CVarDef.Create("net.atmosdbgoverlaytickrate", 3.0f);

    public static readonly CVarDef<float> NetGasOverlayTickRate =
        CVarDef.Create("net.gasoverlaytickrate", 5.0f, CVar.REPLICATED,
        "Gas overlay update rate in TPS. Lower values (e.g. 3-5) reduce network bandwidth at 80+ players.");

    public static readonly CVarDef<int> GasOverlayThresholds =
        CVarDef.Create("net.gasoverlaythresholds", 10, CVar.REPLICATED,
        "Number of opacity levels for gas overlays. Lower values (5-10) improve rendering performance.");
}
