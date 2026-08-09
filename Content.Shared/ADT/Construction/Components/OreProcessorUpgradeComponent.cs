// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class OreProcessorUpgradeComponent : Component
{
    [DataField]
    public float OutputMultiplier = 1f;

    [DataField]
    public float PointsMultiplier = 1f;

    [DataField]
    public ProtoId<MachinePartPrototype> OutputPart = "MatterBin";

    [DataField]
    public ProtoId<MachinePartPrototype> PointsPart = "MicroLaser";
}
