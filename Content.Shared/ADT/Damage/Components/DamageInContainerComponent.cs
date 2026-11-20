// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageInContainerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickTime;

    [DataField("interval")]
    public float Interval = 1f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage = default!;

    [DataField, AutoNetworkedField]
    public GroupHealSpecifier? DamageGroups = default!;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField(required: true)]
    public string SlotId = string.Empty;

    [DataField]
    public List<MobState> AllowedStates = new();

    [DataField]
    public FixedPoint2 DamageCap = 0;
}
