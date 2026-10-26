using Content.Server.Medical.BiomassReclaimer;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

public sealed class BiomassReclaimerMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomassReclaimerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<BiomassReclaimerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, BiomassReclaimerComponent component, RefreshPartsEvent args)
    {
        var speed = args.GetStatMultiplier(MachineStat.Speed);

        component.ExtractMultiplier = args.GetStatMultiplier(MachineStat.Output);
        component.WorkTimeMultiplier = 1f / speed;
    }

    private static void OnUpgradeExamine(EntityUid uid, BiomassReclaimerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-biomass-yield", component.ExtractMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-process-speed", 1f / component.WorkTimeMultiplier, benefit: true);
    }
}
