using Robust.Shared.Configuration;

namespace Content.Goobstation.Common.CCVar;

[CVarDefs]
public sealed partial class GoobCVars
{
    public static readonly CVarDef<float> LightDetectionRange =
        CVarDef.Create("light.detection_range", 10f, CVar.SERVER);

    public static readonly CVarDef<float> LightUpdateFrequency =
        CVarDef.Create("light.detection_update_frequency", 1f, CVar.SERVER);

    public static readonly CVarDef<float> LightMaximumLevel =
        CVarDef.Create("light.maximum_light_level", 10f, CVar.SERVER);

}
