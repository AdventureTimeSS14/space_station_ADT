// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: серво уменьшает требуемые ингредиенты медицинского ассемблера.
/// </summary>
public sealed class AssemblerMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssemblerUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AssemblerUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, AssemblerUpgradeComponent component, RefreshPartsEvent args)
    {
        var servoTier = MathF.Max(1f, args.GetPartRating(MachinePartIds.Servo, 1f));
        // Базовые части (тир 1) без прибавок: Т2 = -10%, Т3 = -20%, Т4 = -30%
        component.IngredientMultiplier = servoTier > 1f ? MathF.Max(0.5f, 1f - (servoTier - 1f) * 0.1f) : 1f;
    }

    private static void OnUpgradeExamine(EntityUid uid, AssemblerUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-assembler-ingredients", component.IngredientMultiplier);
    }
}
