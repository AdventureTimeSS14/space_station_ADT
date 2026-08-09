// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

namespace Content.Shared.ADT.Construction.Components;

/// <summary>
/// ADT: вместимость буффера химмастера (для осмотра улучшений).
/// </summary>
[RegisterComponent]
public sealed partial class ChemMasterBufferComponent : Component
{
    [DataField]
    public float BaseBufferCapacity = 200f;

    [DataField]
    public float BufferCapacity = 200f;
}
