// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Ломка началась, кончилась или сменила стадию - симптомы нужно пересчитать.
/// </summary>
[ByRefEvent]
public readonly record struct AddictionSymptomsChangedEvent(EntityUid Uid);
