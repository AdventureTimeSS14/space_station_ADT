// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Robust.Shared.Audio;

namespace Content.Shared.ADT.Shadow;

[RegisterComponent]
public sealed partial class PhotophobiaComponent : Component
{
    [DataField("damagePerLight")]
    public float DamagePerLight = 0.5f;

    [DataField("interval")]
    public float Interval = 1f;

    [DataField("nextTickTime")]
    public TimeSpan NextTickTime;

    [DataField("damageSound")]
    public SoundSpecifier DamageSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/energy_meat2.ogg");

    [DataField("applyShadowWeakness")]
    public bool ApplyShadowWeakness = false;

    [DataField("speedModifier")]
    public float SpeedModifier = 0.75f;

    [DataField("damageModfier")]
    public float DamageModfier = 0.2f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool ShadowWeakness = false;
}
