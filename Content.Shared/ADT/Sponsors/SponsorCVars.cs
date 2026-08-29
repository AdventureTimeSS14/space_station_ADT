using Robust.Shared.Configuration;

namespace Content.Shared.ADT.Sponsors;

[CVarDefs]
public sealed class SponsorCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("adt.sponsor.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Учитывать ли старую спонсорку при валидации профиля
    /// </summary>
    public static readonly CVarDef<bool> LegacyBridge =
        CVarDef.Create("adt.sponsor.legacy_bridge", true, CVar.SERVERONLY);

    /// <summary>
    /// Отдельный токен для спонсорского HTTP API
    /// </summary>
    public static readonly CVarDef<string> ApiToken =
        CVarDef.Create("adt.sponsor.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL | CVar.ARCHIVE);

    public static readonly CVarDef<bool> EnforceProfile =
        CVarDef.Create("adt.sponsor.enforce_profile", true, CVar.SERVER | CVar.REPLICATED);
}
