// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Chemistry;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: ограниченный буффер химмастера. Серво увеличивает вместимость буффера:
/// база 200, каждый тир +200 (Т1 = 400, Т2 = 600, Т3 = 800, Т4 = 1000).
/// </summary>
public sealed class ChemMasterMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public const float BaseBufferCapacity = 1500f;
    public const float BufferCapacityPerTier = 1500f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ChemMasterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, ChemMasterComponent component, RefreshPartsEvent args)
    {
        var servoTier = args.GetPartRating(MachinePartIds.Servo, 1f);
        // Базовые части (тир 1) без прибавок: база 1500, каждый уровень выше +1500 (Т2 = 3000, Т3 = 4500, Т4 = 6000)
        var capacity = BaseBufferCapacity + BufferCapacityPerTier * MathF.Max(0f, servoTier - 1f);

        if (TryComp<ChemMasterBufferComponent>(uid, out var buffer))
        {
            buffer.BufferCapacity = capacity;
            buffer.BaseBufferCapacity = BaseBufferCapacity;
        }

        if (_solution.TryGetSolution(uid, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            bufferSolution.MaxVolume = capacity;
    }

    private void OnUpgradeExamine(EntityUid uid, ChemMasterComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<ChemMasterBufferComponent>(uid, out var buffer))
            return;

        args.AddPercentageUpgrade("machine-upgrade-chem-master-buffer", buffer.BufferCapacity / buffer.BaseBufferCapacity);
    }
}
