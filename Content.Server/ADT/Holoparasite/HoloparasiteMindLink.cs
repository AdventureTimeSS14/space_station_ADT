using Content.Shared.ADT.Language;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Holoparasite;

/// <summary>
/// Константы ментальной связи голопаразита.
/// </summary>
public static class HoloparasiteMindLink
{
    [ValidatePrototypeId<LanguagePrototype>]
    public const string Language = "ADTHoloparasiteMindLink";
}