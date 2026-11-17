// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Server.EUI;
using Content.Shared.Vampire;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Content.Shared.FixedPoint;

namespace Content.Server.Vampire;

[UsedImplicitly]
public sealed partial class DissectSelectionEui : BaseEui
{
    private readonly EntityUid _vampire;
    private readonly EntityUid _target;
    private readonly FixedPoint2 _blood;
    private readonly VampireSystem _vampireSystem;
    private readonly DissectSelectionEuiState _state;

    public DissectSelectionEui(EntityUid vampire, EntityUid target, FixedPoint2 bloodCost, VampireSystem vampireSystem, DissectSelectionEuiState state)
    {
        _vampire = vampire;
        _target = target;
        _blood = bloodCost;
        _vampireSystem = vampireSystem;
        _state = state;
    }

    public override EuiStateBase GetNewState() => _state;

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is DissectOrganSelectedMessage selected)
        {
            _vampireSystem.StartDissection(_vampire, _target, selected.Target, _blood);
            Close();
        }
    }
}
