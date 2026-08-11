// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Components;

/// <summary>
/// Marks an entity as a machine part (capacitor, servo, matter bin, etc).
/// The part type and tier are used by machines to apply upgrades.
/// </summary>
[RegisterComponent]
public sealed partial class MachinePartComponent : Component
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> Part;

    [DataField]
    public int Tier = 1;
}
