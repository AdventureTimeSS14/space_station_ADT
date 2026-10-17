// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction.Symptoms;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Зависимости моба. Есть у всех игроков со спавна: подсесть может каждый.
/// </summary>
// Каналы меняют серверные системы (AddictionSystem, AdjustAddictionLevelEffectSystem)
// и Shared-эффекты трайтов (AddictionTraitEffect, RandomAddictionEffect), поэтому
// [Access(typeof(...))] осознанно не ставится: эффекты не являются системами.
[RegisterComponent]
public sealed partial class AddictionComponent : Component
{
    [DataField]
    public List<AddictionChannel> Channels = new();

    /// <summary>
    /// Должен быть меньше длительности любого симптома, иначе тот успеет истечь
    /// между продлениями.
    /// </summary>
    [DataField]
    public TimeSpan SymptomRefreshInterval = TimeSpan.FromSeconds(10);

    public TimeSpan NextSymptomRefresh;

    /// <summary>
    /// Что ломка наложила сама: ключ симптома - сам симптом. Снимаем только своё.
    /// </summary>
    public Dictionary<string, AddictionSymptom> AppliedSymptoms = new();

    /// <summary>
    /// Заготовка канала для трайта «случайная зависимость»: вид выберет AddictionSystem
    /// при спавне, из PendingRandomPool минус уже занятые. Null - трайт не взят.
    /// </summary>
    public AddictionChannel? PendingRandomChannel;

    public List<ProtoId<AddictionPrototype>>? PendingRandomPool;
}
