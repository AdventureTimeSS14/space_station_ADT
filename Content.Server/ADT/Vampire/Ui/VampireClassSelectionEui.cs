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

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireClassSelectionEui : BaseEui
{
    private readonly EntityUid _vampire;
    private readonly VampireSystem _vampireSystem;

    public VampireClassSelectionEui(EntityUid vampire, VampireSystem vampireSystem)
    {
        _vampire = vampire;
        _vampireSystem = vampireSystem;
    }

    public override EuiStateBase GetNewState() => new VampireClassSelectionState();

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is VampireClassSelectedMessage selected)
        {
            _vampireSystem.OnClassSelected(_vampire, selected.SelectedClass);
            Close();
        }
    }
}
