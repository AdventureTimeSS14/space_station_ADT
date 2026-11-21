// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Vampire.Components;

/// <summary>
/// The target is a supreme vampire.
/// </summary>
[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SupremeVampireComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Active = false;
}
