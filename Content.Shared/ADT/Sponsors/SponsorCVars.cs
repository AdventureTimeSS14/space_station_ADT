using Robust.Shared.Configuration;

namespace Content.Shared.ADT.Sponsors;

[CVarDefs]
public sealed class SponsorCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("adt.sponsor.enabled", false, CVar.SERVERONLY);

    /// <summary>
    /// Учитывать ли старую спонсорку при валидации профиля
    /// </summary>
    public static readonly CVarDef<bool> LegacyBridge =
        CVarDef.Create("adt.sponsor.legacy_bridge", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Отдельный токен для спонсорского HTTP API
    /// </summary>
    public static readonly CVarDef<string> ApiToken =
        CVarDef.Create("adt.sponsor.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL | CVar.ARCHIVE);

    public static readonly CVarDef<bool> EnforceProfile =
        CVarDef.Create("adt.sponsor.enforce_profile", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> ColorsRateLimitPeriod =
        CVarDef.Create("adt.sponsor.colors_rate_limit_period", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<int> ColorsRateLimitCount =
        CVarDef.Create("adt.sponsor.colors_rate_limit_count", 20, CVar.SERVERONLY);
}
