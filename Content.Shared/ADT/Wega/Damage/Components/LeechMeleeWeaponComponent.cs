// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LeechMeleeWeaponComponent : Component
{
    [DataField]
    public DamageSpecifier? Heal = default!;

    [DataField]
    public GroupHealSpecifier? HealGroups = default!;

    [DataField]
    public float ModifyBloodLevel = 0.0f;

    [DataField]
    public float BloodlossModifier = 0.0f;

    [DataField]
    public bool Weighted = false;

    [DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };

    [DataField]
    public EntityWhitelist? Blacklist;
}
