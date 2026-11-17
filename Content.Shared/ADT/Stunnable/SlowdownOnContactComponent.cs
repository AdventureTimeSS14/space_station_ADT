// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Content.Shared.Stunnable;

namespace Content.Shared.ADT.Stunnable;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class SlowdownOnContactComponent : Component
{
    [DataField]
    public string FixtureId = "fix";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public EntityWhitelist Blacklist = new();
}
