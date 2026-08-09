// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Cloning;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: машинные части для клонаппарата. Серво ускоряет клонирование,
/// сканмодуль снижает шанс неудачного клонирования.
/// </summary>
public sealed class CloningPodMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CloningPodComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<CloningPodComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, CloningPodComponent component, RefreshPartsEvent args)
    {
        var servoTier = MathF.Max(1f, args.GetPartRating(MachinePartIds.Servo, 1f));
        var scanTier = MathF.Max(1f, args.GetPartRating(MachinePartIds.ScanningModule, 1f));

        // Базовые части (тир 1) без прибавок
        component.SpeedMultiplier = servoTier;
        component.CloningSafety = scanTier;
    }

    private static void OnUpgradeExamine(EntityUid uid, CloningPodComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-cloning-speed", component.SpeedMultiplier);
        args.AddPercentageUpgrade("machine-upgrade-cloning-safety", component.CloningSafety);
    }
}
