// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem
{
    public static readonly EntProtoId SlowdownEffect = "BasicSlowdownStatusEffect";

    [Dependency] private MovementModStatusSystem _movementMod = default!;

    private void OnSlowdownOnContactCollide(Entity<SlowdownOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        _movementMod.TryUpdateMovementSpeedModDuration(args.OtherEntity, SlowdownEffect, ent.Comp.Duration, ent.Comp.Multiplier);
    }
}
