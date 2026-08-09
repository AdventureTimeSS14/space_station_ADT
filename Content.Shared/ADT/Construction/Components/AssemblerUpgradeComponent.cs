// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

namespace Content.Shared.ADT.Construction.Components;

/// <summary>
/// ADT: апгрейд медицинского ассемблера. Серво уменьшает требуемые ингредиенты.
/// </summary>
[RegisterComponent]
public sealed partial class AssemblerUpgradeComponent : Component
{
    /// <summary>
    /// Множитель требуемых ингредиентов (Т1 = 1, Т2 = 0.9, Т3 = 0.8, Т4 = 0.7).
    /// </summary>
    [DataField]
    public float IngredientMultiplier = 1f;
}
