// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction.Effects;

/// <summary>
/// Реагентный эффект зависимости: отрицательный Amount лечит (Детоксин),
/// положительный качает канал (передоз лекарств).
/// </summary>
public sealed partial class AdjustAddictionLevel : EntityEffectBase<AdjustAddictionLevel>
{
    /// <summary>
    /// Изменение уровня за цикл метаболизма.
    /// </summary>
    [DataField]
    public float Amount = -1f;

    /// <summary>
    /// Какую зависимость менять (id AddictionPrototype). Null - все сразу (только для лечения).
    /// </summary>
    [DataField]
    public ProtoId<AddictionPrototype>? Kind;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-adjust-addiction-level", ("amount", MathF.Abs(Amount)));
}
