// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Server.Anomaly.Components;

namespace Content.Server.Anomaly;

public sealed partial class AnomalySystem
{
    // ADT-Tweak: машинные части (сосуд аномалий)
    public void SetPointMultiplier(EntityUid uid, float multiplier, AnomalyVesselComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.PointMultiplier = multiplier;
    }
}
