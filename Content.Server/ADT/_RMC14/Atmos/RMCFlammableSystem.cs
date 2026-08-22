// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), AGPL-3.0-or-later.
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._RMC14.Atmos;
using Content.Shared.ActionBlocker;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Atmos;

public sealed class RMCFlammableSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public void DoStopDropRollAnimation(EntityUid uid, TimeSpan length)
    {
        if (!HasComp<RMCStopDropRollVisualsComponent>(uid))
            return;

        if (!_actionBlocker.CanMove(uid))
            return;

        RaiseNetworkEvent(new RMCStopDropRollVisualsNetworkEvent(GetNetEntity(uid), length), Filter.Pvs(uid));
    }
}
