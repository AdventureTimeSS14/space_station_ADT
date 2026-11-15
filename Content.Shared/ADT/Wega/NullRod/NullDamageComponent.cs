// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.NullRod.Components;

/// <summary>
/// Indicates the presence and amount of zero damage in an unholy entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NullDamageComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextNullDamageTick { get; set; }

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 NullDamage = 0;

    [DataField]
    public FixedPoint2 MaxNullDamage = 120;

    [DataField]
    public FixedPoint2 NullDamageRecoveryPerTick = 2;

    [DataField]
    public float NullDamageRecoveryInterval = 2f;
}
