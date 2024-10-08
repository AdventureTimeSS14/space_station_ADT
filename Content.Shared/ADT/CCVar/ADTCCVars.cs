using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

[CVarDefs]
public sealed class ADTCCVars
{
    /*
    * Barks
    */
    public static readonly CVarDef<bool> BarksEnabled =
        CVarDef.Create("barks.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMaxPitch =
        CVarDef.Create("barks.max_pitch", 1.5f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMinPitch =
        CVarDef.Create("barks.min_pitch", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMinDelay =
        CVarDef.Create("barks.min_delay", 0.1f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksMaxDelay =
        CVarDef.Create("barks.max_delay", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ReplaceTTSWithBarks =
        CVarDef.Create("barks.replace_tts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("barks.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BoomBoxVolume =
        CVarDef.Create("audio.boomboxvolume", 1f, CVar.CLIENTONLY | CVar.REPLICATED);

    /*
    * Radial menu
    */
    public static readonly CVarDef<bool> CenterRadialMenu =
        CVarDef.Create("radialmenu.center", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
