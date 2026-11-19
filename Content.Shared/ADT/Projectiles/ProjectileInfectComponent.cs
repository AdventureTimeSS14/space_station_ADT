// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.ADT.Virology.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileInfectComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ADTDiseasePrototype> Infection;

    [DataField]
    public float Prob = 0.1f;
}