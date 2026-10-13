// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction.Sources;

/// <summary>
/// Правило «этот реаген кормит эту зависимость». Живёт на стороне прототипа зависимости,
/// а не на реагенте: сами реагенты лежат в офф-файлах, трогать их не нужно.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class AddictionSource
{
    /// <summary>
    /// Подходит ли реаген. Прототип может не найтись (реаген удалён) - тогда proto равен null.
    /// </summary>
    public abstract bool Matches(ReagentId reagent, ReagentPrototype? proto);
}

/// <summary>
/// Конкретный реаген по id (никотин: своей группы метаболизма у него нет).
/// </summary>
public sealed partial class AddictionReagentSource : AddictionSource
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    public override bool Matches(ReagentId reagent, ReagentPrototype? proto)
        => reagent.Prototype == Reagent.Id;
}

/// <summary>
/// Все реагенты одной группы (ReagentPrototype.Group), например Narcotics.
/// </summary>
public sealed partial class AddictionReagentGroupSource : AddictionSource
{
    [DataField(required: true)]
    public string Group = string.Empty;

    public override bool Matches(ReagentId reagent, ReagentPrototype? proto)
        => proto != null && proto.Group == Group;
}

/// <summary>
/// Все наследники прототипа-родителя, например BaseAlcohol.
/// </summary>
public sealed partial class AddictionReagentParentSource : AddictionSource
{
    [DataField(required: true)]
    public string Parent = string.Empty;

    public override bool Matches(ReagentId reagent, ReagentPrototype? proto)
        => proto?.Parents != null && proto.Parents.Contains(Parent);
}
