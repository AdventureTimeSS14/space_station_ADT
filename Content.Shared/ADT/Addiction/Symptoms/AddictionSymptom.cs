// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using JetBrains.Annotations;

namespace Content.Shared.ADT.Addiction.Symptoms;

/// <summary>
/// Симптом ломки: держится на мобе, пока стадия его требует.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class AddictionSymptom
{
    /// <summary>
    /// Чем симптом владеет на мобе. По ключу его снимают, поэтому он должен быть стабилен.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// False - симптом не стал трогать моба, снимать его тоже не будут.
    /// </summary>
    public abstract bool Apply(AddictionSymptomContext ctx);

    public abstract void Remove(AddictionSymptomContext ctx);
}

/// <summary>
/// Owned - ломка уже держит этот симптом, вызов идёт на продление.
/// </summary>
public readonly record struct AddictionSymptomContext(
    EntityUid Target,
    IEntityManager EntMan,
    bool Owned);
