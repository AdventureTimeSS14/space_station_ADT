// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Стадии тяжести зависимости: 0 - нет, 1 - лёгкая, 2 - средняя, 3 - тяжёлая.
/// Определяются по уровню привыкания (Level 0..100), используются и сервером
/// (симптомы ломки, анализатор), и клиентом (отображение).
/// </summary>
public static class AddictionStage
{
    public const float MildThreshold = 25f;
    public const float MediumThreshold = 50f;
    public const float SevereThreshold = 75f;

    public static int FromLevel(float level)
        => level >= SevereThreshold ? 3
         : level >= MediumThreshold ? 2
         : level >= MildThreshold ? 1
         : 0;
}
