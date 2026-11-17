// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.FixedPoint;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireBloodAbsorptionComponent : Component
{
    [DataField]
    public FixedPoint2 BloodStealAmount = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid VampireOwner = default!;
}
