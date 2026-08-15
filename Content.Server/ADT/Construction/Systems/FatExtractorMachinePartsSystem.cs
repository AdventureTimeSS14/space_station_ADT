using Content.Server.Nutrition.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

public sealed class FatExtractorMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FatExtractorComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<FatExtractorComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, FatExtractorComponent component, RefreshPartsEvent args)
    {
        component.NutritionMultiplier = args.GetStatMultiplier(MachineStat.Speed);
    }

    private static void OnUpgradeExamine(EntityUid uid, FatExtractorComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-fat-extraction", component.NutritionMultiplier, benefit: true);
    }
}
