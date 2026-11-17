// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

namespace Content.Shared.Vampire.Components;

/// <summary>
/// A mark that allows you to see the presence of fang marks on the victim.
/// </summary>
[RegisterComponent]
public sealed partial class BittenByVampireComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExpirationTime { get; set; }

    [DataField]
    public float LifetimeSeconds = 900f;
}
