// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

namespace Content.Shared.Vampire.Components;

/// <summary>
/// Just a chill guy.
/// Defines an entity as a beacon of the soul.
/// </summary>
[RegisterComponent]
public sealed partial class BeaconSoulComponent : Component
{
    [DataField]
    public EntityUid VampireOwner = default!;
}
