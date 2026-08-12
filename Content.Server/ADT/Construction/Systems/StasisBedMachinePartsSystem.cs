using Content.Server.Power.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Bed.Components;

namespace Content.Server.ADT.Construction.Systems;

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

        upgrade.PowerMultiplier = args.GetStatMultiplier(MachineStat.PowerDraw);

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            receiver.Load = upgrade.PowerLoad * upgrade.PowerMultiplier;
    }

    private void OnUpgradeExamine(EntityUid uid, StasisBedComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<StasisBedMachinePartComponent>(uid, out var upgrade))
            return;

        args.AddPercentageUpgrade("machine-upgrade-stasis-bed-power", upgrade.PowerMultiplier, benefit: true);
    }
}
