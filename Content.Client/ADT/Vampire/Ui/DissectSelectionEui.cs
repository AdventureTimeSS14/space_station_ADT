// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Vampire;
using JetBrains.Annotations;

namespace Content.Client._Wega.Vampire.Ui;

[UsedImplicitly]
public sealed partial class DissectSelectionEui : BaseEui
{
    private DissectSelectionMenu _menu;

    public DissectSelectionEui()
    {
        _menu = new DissectSelectionMenu();
        _menu.OnOrganSelected += selectedOrgan =>
        {
            SendMessage(new DissectOrganSelectedMessage(selectedOrgan));
            _menu.Close();
        };
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        _menu.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);
        if (state is not DissectSelectionEuiState dissectState)
            return;

        _menu.Populate(dissectState);
    }
}
