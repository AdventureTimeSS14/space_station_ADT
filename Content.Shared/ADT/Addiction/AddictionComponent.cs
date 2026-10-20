// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction.Symptoms;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Зависимости моба. Есть у всех игроков со спавна.
/// </summary>
[RegisterComponent]
public sealed partial class AddictionComponent : Component
{
    [DataField]
    public List<AddictionChannel> Channels = new();

    /// <summary>
    /// Должен быть меньше длительности любого симптома.
    /// </summary>
    [DataField]
    public TimeSpan SymptomRefreshInterval = TimeSpan.FromSeconds(10);

    public TimeSpan NextSymptomRefresh;

    public Dictionary<string, AddictionSymptom> AppliedSymptoms = new();

    /// <summary>
    /// Вид выберет AddictionSystem при спавне, из PendingRandomPool минус уже занятые.
    /// </summary>
    public AddictionChannel? PendingRandomChannel;

    public List<ProtoId<AddictionPrototype>>? PendingRandomPool;
}
