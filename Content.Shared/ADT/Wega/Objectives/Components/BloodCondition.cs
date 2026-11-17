// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(BloodConditionSystem))]
public sealed partial class BloodConditionComponent : Component
{
    public Dictionary<EntityUid, float> BloodTargets = new();
	
    [DataField]
    public int TargetMin;

    [DataField]
    public int TargetMax;
}
