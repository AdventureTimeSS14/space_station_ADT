// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Server.EUI;
using Content.Shared.Vampire;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server.Vampire;

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
