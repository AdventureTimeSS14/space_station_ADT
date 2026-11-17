// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Vampire.Components;

[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class BestiaContainerComponent : Component
{
    public const string ContainerId = "organs_container";

    [ViewVariables]
    public Container OrgansContainer = default!;

    // Mew Mew Mew Mew Mew Mew Mew Mew~!
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, int> OrgansExtractedFromVictim = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public int MaxRegularOrgans = 10;

    [ViewVariables(VVAccess.ReadOnly)]
    public int MaxCriticalOrgans = 2;

    [ViewVariables(VVAccess.ReadOnly)]
    public int MaxOrgansPerVictim = 1;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<float> UnlockedCriticalThresholds = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public List<float> UnlockedVictimThresholds = new();

    [DataField("criticalThresholds")]
    public Dictionary<float, int> CriticalOrganThresholds { get; set; } = new()
    {
        { 600f, 2 },
        { 1000f, 2 }
    };

    [DataField("organsThresholds")]
    public Dictionary<float, int> OrgansPerVictimThresholds { get; set; } = new()
    {
        { 600f, 1 },
        { 1000f, 1 }
    };
}
