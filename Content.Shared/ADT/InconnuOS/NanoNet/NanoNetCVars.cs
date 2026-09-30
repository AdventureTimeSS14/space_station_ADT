using Robust.Shared.Configuration;

namespace Content.Shared.ADT.InconnuOS.NanoNet;

[CVarDefs]
public sealed class NanoNetCVars
{
    public static readonly CVarDef<int> MaxSiteLength =
        CVarDef.Create("adt.nanonet.max_site_length", 32768, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> MaxSitesPerOwner =
        CVarDef.Create("adt.nanonet.max_sites_per_owner", 3, CVar.SERVERONLY);

    public static readonly CVarDef<int> MaxSitesTotal =
        CVarDef.Create("adt.nanonet.max_sites_total", 256, CVar.SERVERONLY);

    public static readonly CVarDef<int> MaxUrlLength =
        CVarDef.Create("adt.nanonet.max_url_length", 256, CVar.SERVERONLY);

    public static readonly CVarDef<float> PublishCooldown =
        CVarDef.Create("adt.nanonet.publish_cooldown", 15f, CVar.SERVERONLY);
}
