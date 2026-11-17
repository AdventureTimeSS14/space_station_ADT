// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Damage.Prototypes;
using Content.Shared.Metabolism;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

/// <summary>
/// Stores the creature's original data before it becomes a vampire.
/// Used for restoration when the vampire component is removed.
/// </summary>
[RegisterComponent, Access(typeof(SharedVampireSystem))]
public sealed partial class VampireOriginalStateComponent : Component
{
    [DataField]
    public HashSet<Type> RemovedComponents = new();

    [DataField]
    public Dictionary<EntityUid, HashSet<ProtoId<MetabolizerTypePrototype>>> OriginalMetabolizerTypes = new();

    [DataField]
    public Satiation? OriginalThirst;

    [DataField]
    public float? OriginalColdDamageThreshold;

    [DataField]
    public ProtoId<DamageModifierSetPrototype>? OriginalDamageModifierSetId;

    [DataField]
    public Color? OriginalEyeColor;

    [DataField]
    public bool? OriginalDrawFov;

    [DataField]
    public float? OriginalNightVisionRadius;

    [DataField]
    public Color? OriginalNightVisionColor;
}
