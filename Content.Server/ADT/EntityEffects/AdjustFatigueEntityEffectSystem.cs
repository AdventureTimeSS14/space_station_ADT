// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Fatigue;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.ADT.EntityEffects;

/// <summary>
/// Эффект реагентов: корректирует стадию усталости (кофеин бодрит, нашатырь будит от коллапса).
/// </summary>
public sealed partial class AdjustFatigueEntityEffectSystem : EntityEffectSystem<FatigueComponent, AdjustFatigue>
{
    [Dependency] private SharedFatigueSystem _fatigue = default!;

    protected override void Effect(Entity<FatigueComponent> entity, ref EntityEffectEvent<AdjustFatigue> args)
    {
        var delta = (int)MathF.Round(args.Effect.Stages * args.Scale);
        if (delta == 0 && args.Effect.Stages < 0)
            delta = -1;

        if (args.Effect.ResetToAlert)
        {
            _fatigue.ClearFatigueSleep(entity);
            _fatigue.ResetToAlert(entity, rollNewTimer: true);
            return;
        }

        _fatigue.AdjustStage(entity.Owner, delta);
    }
}
