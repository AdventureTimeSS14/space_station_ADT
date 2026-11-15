// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileAoEComponent : Component
{
    [DataField] public float DamageRadius = 3f;
    [DataField] public float DamageMultiplier = 0.5f;
}