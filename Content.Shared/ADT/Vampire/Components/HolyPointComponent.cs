// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

namespace Content.Shared.Vampire.Components;

/// <summary>
/// A component for testing vampire arson near holy sites.
/// </summary>
[RegisterComponent]
public sealed partial class HolyPointComponent : Component
{
    [DataField]
    public float Range = 6f;

    public float NextTimeTick { get; set; }
}
