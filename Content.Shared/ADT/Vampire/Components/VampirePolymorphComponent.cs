// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

namespace Content.Shared.Vampire.Components;

[RegisterComponent, Access(typeof(SharedVampireSystem))]
public sealed partial class VampirePolymorphComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Body;
}
