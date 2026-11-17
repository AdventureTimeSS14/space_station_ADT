// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Server.EUI;
using Content.Shared.ADT.Vampire;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server.ADT.Vampire;

[UsedImplicitly]
public sealed partial class TrophiesMenuEui : BaseEui
{
    private readonly TrophiesEuiState _state;

    public TrophiesMenuEui(TrophiesEuiState state)
    {
        _state = state;
    }

    public override EuiStateBase GetNewState() => _state;
}
