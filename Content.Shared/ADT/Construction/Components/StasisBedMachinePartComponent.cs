// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

namespace Content.Shared.ADT.Construction.Components;

/// <summary>
/// Machine-part upgrade values for the stasis bed. Lives in its own component because
/// the vanilla StasisBedComponent is only writable by BedSystem.
/// </summary>
[RegisterComponent]
public sealed partial class StasisBedMachinePartComponent : Component
{
    [DataField]
    public float PowerLoad = 1000f;

    [DataField]
    public float PowerMultiplier = 1f;
}
