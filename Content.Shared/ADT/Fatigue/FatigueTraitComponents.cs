// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.ADT.Fatigue;

/// <summary>
/// Трайт «Бодрость»: увеличивает время до первой стадии усталости и между стадиями.
/// </summary>
[RegisterComponent]
public sealed partial class EnergeticFatigueTraitComponent : Component
{
    /// <summary>Множитель длительности стадий усталости (2 = в два раза медленнее устаёт).</summary>
    [DataField]
    public float StageDurationMultiplier = 2f;
}

/// <summary>
/// Трайт «Сонливый»: ускоряет наступление стадий усталости.
/// </summary>
[RegisterComponent]
public sealed partial class SleepyFatigueTraitComponent : Component
{
    /// <summary>Множитель длительности стадий усталости (0.5 = в два раза быстрее устаёт).</summary>
    [DataField]
    public float StageDurationMultiplier = 0.5f;
}
