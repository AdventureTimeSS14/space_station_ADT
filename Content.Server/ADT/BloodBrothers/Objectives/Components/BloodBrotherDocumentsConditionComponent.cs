// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Shared.ADT.BloodBrothers.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.BloodBrothers.Objectives.Components;

[RegisterComponent]
public sealed partial class BloodBrotherDocumentsConditionComponent : Component
{
    public List<EntityUid> TeamMinds = new();

    [DataField]
    public EntProtoId StartingFolder = default!;

    [DataField]
    public EntProtoId RequiredFolder = default!;

    [DataField]
    public int Variant;
}
