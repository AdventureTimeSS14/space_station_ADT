// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction.Symptoms;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Зависимости моба. Есть у всех игроков со спавна: подсесть может каждый.
/// Хранит только состояние - вся настройка лежит в AddictionPrototype.
/// </summary>
// Каналы меняют серверные системы (AddictionSystem, AdjustAddictionLevelEffectSystem)
// и Shared-эффекты трайтов (AddictionTraitEffect, RandomAddictionEffect), поэтому
// [Access(typeof(...))] осознанно не ставится: эффекты не являются системами.
[RegisterComponent]
public sealed partial class AddictionComponent : Component
{
    /// <summary>
    /// Каналы зависимости. Каждый живёт своей жизнью: свой уровень, своя ломка.
    /// </summary>
    [DataField]
    public List<AddictionChannel> Channels = new();

    /// <summary>
    /// Как часто симптомы ломки продлеваются. Технический тик, а не тюнинг зависимости:
    /// должен быть меньше длительности любого симптома, иначе тот успеет истечь.
    /// </summary>
    [DataField]
    public TimeSpan SymptomRefreshInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Время следующего продления симптомов.
    /// </summary>
    public TimeSpan NextSymptomRefresh;

    /// <summary>
    /// Что ломка наложила сама: ключ симптома - сам симптом.
    /// Снимаем только своё, чужие статус-эффекты и компоненты не трогаем.
    /// </summary>
    public Dictionary<string, AddictionSymptom> AppliedSymptoms = new();

    /// <summary>
    /// Заготовка канала для трайта «случайная зависимость»: вид выберет AddictionSystem
    /// при спавне, из PendingRandomPool минус уже занятые. Null - трайт не взят.
    /// </summary>
    public AddictionChannel? PendingRandomChannel;

    /// <summary>
    /// Из чего выбирать случайную зависимость.
    /// </summary>
    public List<ProtoId<AddictionPrototype>>? PendingRandomPool;
}
