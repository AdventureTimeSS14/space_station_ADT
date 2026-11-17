// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Vampire;

[Serializable, NetSerializable]
public sealed partial class VampireClassSelectionState : EuiStateBase
{
}

[Serializable, NetSerializable]
public sealed partial class VampireClassSelectedMessage : EuiMessageBase
{
    public VampireClassEnum SelectedClass { get; }
    public VampireClassSelectedMessage(VampireClassEnum selectedClass) => SelectedClass = selectedClass;
}
