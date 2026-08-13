// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction.Effects;

/// <summary>
/// Реагентный эффект зависимости: отрицательный Amount лечит (Детоксин),
/// положительный Amount качает канал (передоз лекарств). Когда уровень падает ниже порога,
/// AddictionSystem снимает зависимость.
/// </summary>
public sealed partial class AdjustAddictionLevel : EntityEffectBase<AdjustAddictionLevel>
{
    /// <summary>
    /// На сколько меняется уровень привыкания за цикл метаболизма (отрицательное - лечение).
    /// </summary>
    [DataField]
    public float Amount = -1f;

    /// <summary>
    /// Какой канал менять. Null - все сразу (только для лечения).
    /// </summary>
    [DataField]
    public AddictionKind? Kind;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-adjust-addiction-level", ("amount", MathF.Abs(Amount)));
}
