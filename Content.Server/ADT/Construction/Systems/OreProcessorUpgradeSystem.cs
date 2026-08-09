// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: applies machine-part upgrades to the ore processor: more output per smelt.
/// </summary>
public sealed class OreProcessorUpgradeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreProcessorUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<OreProcessorUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, OreProcessorUpgradeComponent component, RefreshPartsEvent args)
    {
        var tier = args.GetPartRating(component.OutputPart);
        // Базовые части (тир 1) без прибавок: Т1 = x1, Т2 = x2, Т3 = x3, Т4 = x4
        component.OutputMultiplier = tier;
    }

    private static void OnUpgradeExamine(EntityUid uid, OreProcessorUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-ore-output", component.OutputMultiplier);
    }
}
