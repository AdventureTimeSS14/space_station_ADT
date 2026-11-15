// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(DiablerieConditionSystem))]
public sealed partial class DiablerieConditionComponent : Component
{
    public Dictionary<EntityUid, float> BloodTargets = new();
}
