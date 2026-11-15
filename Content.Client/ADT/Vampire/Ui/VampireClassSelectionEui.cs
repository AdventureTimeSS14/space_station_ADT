// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Client.Eui;
using Content.Shared.Vampire;
using JetBrains.Annotations;

namespace Content.Client._Wega.Vampire.Ui;

[UsedImplicitly]
public sealed partial class VampireClassSelectionEui : BaseEui
{
    private readonly VampireClassSelectionMenu _menu;

    public VampireClassSelectionEui()
    {
        _menu = new VampireClassSelectionMenu();
        _menu.OnClassSelected += className =>
        {
            SendMessage(new VampireClassSelectedMessage(className));
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
}
