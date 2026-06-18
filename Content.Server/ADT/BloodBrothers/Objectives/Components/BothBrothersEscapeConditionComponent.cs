// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

namespace Content.Server.ADT.BloodBrothers.Objectives.Components;

[RegisterComponent]
public sealed partial class BothBrothersEscapeConditionComponent : Component
{
    public List<EntityUid> TeamMinds = new();
}
