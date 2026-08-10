// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Fatigue;

namespace Content.Client.ADT.Fatigue;

/// <summary>
/// Клиентская часть усталости. Логики нет: скорость, размытие и алерт приходят по сети.
/// </summary>
public sealed partial class FatigueSystem : SharedFatigueSystem;
