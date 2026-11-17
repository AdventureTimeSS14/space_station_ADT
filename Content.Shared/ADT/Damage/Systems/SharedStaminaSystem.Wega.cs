// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Damage.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedStaminaSystem
{
    public void RemoveStaminaDamage(Entity<StaminaComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.StaminaDamage >= ent.Comp.CritThreshold)
            ExitStamCrit(ent);

        ent.Comp.StaminaDamage = 0;
        AdjustStatus(ent.Owner);
        RemComp<ActiveStaminaComponent>(ent);
        _status.TryRemoveStatusEffect(ent, StaminaLow);
        UpdateStaminaVisuals((ent.Owner, ent.Comp));
        Dirty(ent);
    }
}