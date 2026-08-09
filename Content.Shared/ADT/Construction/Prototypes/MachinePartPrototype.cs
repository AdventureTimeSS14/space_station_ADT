// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Prototypes;

[Prototype("machinePart")]
public sealed partial class MachinePartPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public EntProtoId StockPartPrototype = string.Empty;
}
