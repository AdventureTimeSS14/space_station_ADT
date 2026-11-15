// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

namespace Content.Shared.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgicalSkillComponent : Component
{
    [DataField("modifier")]
    public float Modifier = 0.5f;
}
