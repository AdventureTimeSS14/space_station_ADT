// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.ADT.Virology.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileInfectComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ADTDiseasePrototype> Infection;

    [DataField]
    public float Prob = 0.1f;
}