// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Рейзится, когда набор симптомов зависимости изменился: ломка началась или закончилась,
/// сменилась стадия, доза сняла ломку. Подписчики пересчитывают симптомы по всем каналам.
/// </summary>
[ByRefEvent]
public readonly record struct AddictionSymptomsChangedEvent(EntityUid Uid);
