// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustFatigue : EntityEffectBase<AdjustFatigue>
{
    /// <summary>Дельта стадий усталости (отрицательная = бодрит).</summary>
    [DataField]
    public int Stages = -1;

    /// <summary>Если true, полностью сбрасывает усталость на стадию 0 и будит от коллапса.</summary>
    [DataField]
    public bool ResetToAlert;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-adjust-fatigue",
            ("chance", Probability),
            ("stages", Stages),
            ("reset", ResetToAlert));
    }
}
