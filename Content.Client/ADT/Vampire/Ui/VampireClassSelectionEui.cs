// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Client.Eui;
using JetBrains.Annotations;
using Content.Shared.ADT.Vampire;

namespace Content.Client.ADT.Vampire.Ui;

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
