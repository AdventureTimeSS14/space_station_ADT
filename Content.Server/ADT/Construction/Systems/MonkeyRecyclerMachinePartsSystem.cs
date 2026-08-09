// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: машинные части для переработчика макак. Материя-бин увеличивает
/// количество кубов с одного трупа (Т1 = 1, Т2 = 2, Т3 = 3, Т4 = 4).
/// </summary>
public sealed class MonkeyRecyclerMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyMonkeyRecyclerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<XenobiologyMonkeyRecyclerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, XenobiologyMonkeyRecyclerComponent component, RefreshPartsEvent args)
    {
        var matterTier = MathF.Max(1f, args.GetPartRating(MachinePartIds.MatterBin, 1f));
        component.CubeProduction = (int)matterTier;
    }

    private static void OnUpgradeExamine(EntityUid uid, XenobiologyMonkeyRecyclerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-monkey-output", component.CubeProduction);
    }
}
