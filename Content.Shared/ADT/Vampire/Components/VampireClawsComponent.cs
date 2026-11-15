// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireClawsComponent : Component
{
    [DataField]
    public FixedPoint2 BloodStealAmount = 5;

    [DataField,]
    public DamageSpecifier Damage = default!;

    [DataField]
    public float ModifyBloodLevel = 15.0f;

    [DataField]
    public float BloodlossModifier = -2.5f;

    [DataField]
    public float StaminaMod = -10f;
}
