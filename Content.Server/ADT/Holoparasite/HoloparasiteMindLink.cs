// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

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