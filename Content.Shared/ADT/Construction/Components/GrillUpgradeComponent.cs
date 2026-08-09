// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

namespace Content.Shared.ADT.Construction.Components;

/// <summary>
/// ADT: апгрейд электрического гриля (серво увеличивает мощность нагрева).
/// </summary>
[RegisterComponent]
public sealed partial class GrillUpgradeComponent : Component
{
    [DataField]
    public float BasePower = 2400f;

    [DataField]
    public float PowerMultiplier = 1f;
}
