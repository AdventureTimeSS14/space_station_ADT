// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Server.Power.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Bed.Components;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// Machine-part upgrades for the stasis bed. Lives on the server because
/// ApcPowerReceiverComponent is server-side.
/// </summary>
public sealed class StasisBedMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StasisBedComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<StasisBedComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, StasisBedComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<StasisBedMachinePartComponent>(uid, out var upgrade))
            return;

        var capacitorTier = args.GetPartRating(MachinePartIds.Capacitor);
        // ADT-Tweak: базовые части (тир 1) не дают прибавок
        upgrade.PowerLoad = upgrade.BasePowerLoad * (capacitorTier > 1f ? MathF.Max(0.5f, 1f - (capacitorTier - 1f) * 0.1f) : 1f);

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            receiver.Load = upgrade.PowerLoad;
    }

    private void OnUpgradeExamine(EntityUid uid, StasisBedComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<StasisBedMachinePartComponent>(uid, out var upgrade))
            return;

        args.AddPercentageUpgrade("machine-upgrade-stasis-bed-power", upgrade.PowerLoad / upgrade.BasePowerLoad);
    }
}
