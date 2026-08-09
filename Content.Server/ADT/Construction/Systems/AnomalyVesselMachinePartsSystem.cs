// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Server.Anomaly;
using Content.Server.Anomaly.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: машинные части для сосуда аномалий. Каждый микро-лазер даёт +5% к очкам
/// за тир детали (3 лазера Т1 = +15%, Т4 = +60% и т.д. по сумме тиров).
/// </summary>
public sealed class AnomalyVesselMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyVesselComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AnomalyVesselComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, AnomalyVesselComponent component, RefreshPartsEvent args)
    {
        var laserTierSum = args.GetPartRatingSum(MachinePartIds.MicroLaser);
        _anomaly.SetPointMultiplier(uid, 1f + 0.05f * laserTierSum, component);
    }

    private static void OnUpgradeExamine(EntityUid uid, AnomalyVesselComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-anomaly-points", component.PointMultiplier);
    }
}
