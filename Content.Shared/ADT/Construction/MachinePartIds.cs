// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction;

/// <summary>
/// Well-known machine part type ids.
/// </summary>
public static class MachinePartIds
{
    public static readonly ProtoId<MachinePartPrototype> Capacitor = "Capacitor";
    public static readonly ProtoId<MachinePartPrototype> Servo = "Servo";
    public static readonly ProtoId<MachinePartPrototype> MatterBin = "MatterBin";
    public static readonly ProtoId<MachinePartPrototype> ScanningModule = "ScanningModule";
    public static readonly ProtoId<MachinePartPrototype> MicroLaser = "MicroLaser";
}
