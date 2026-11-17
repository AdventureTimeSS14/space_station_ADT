// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Robust.Shared.GameStates;

namespace Content.Shared.Vampire.Components;

/// <summary>
/// Determines whether an entity is the owner of the tralls and allows them to be manipulated.
/// </summary>
[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ThrallOwnerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int MaxThrallCount = 1;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<EntityUid> ThrallOwned = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool DamageSharing = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<float> UnlockedThresholds = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<float, int> ThrallCountThresholds { get; set; } = new()
    {
        { 400f, 1 },
        { 600f, 1 },
        { 1000f, 1 }
    };
}
