// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// ADT: серво увеличивает мощность нагрева электрического гриля (скорость готовки).
/// </summary>
public sealed class GrillMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityHeaterSystem _heater = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrillUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<GrillUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, GrillUpgradeComponent component, RefreshPartsEvent args)
    {
        var servoTier = MathF.Max(1f, args.GetPartRating(MachinePartIds.Servo, 1f));
        component.PowerMultiplier = servoTier;

        if (TryComp<EntityHeaterComponent>(uid, out var heater))
            _heater.SetPower(uid, component.BasePower * servoTier, heater);
    }

    private static void OnUpgradeExamine(EntityUid uid, GrillUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-cook-speed", component.PowerMultiplier);
    }
}
