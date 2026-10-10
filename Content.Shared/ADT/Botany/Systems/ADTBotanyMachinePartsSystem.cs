using Content.Shared.ADT.Botany.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Botany.Components;

namespace Content.Shared.ADT.Botany.Systems;

public sealed class ADTBotanyMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTPlantTrayUpgradeComponent, RefreshPartsEvent>(OnTrayRefreshParts);
        SubscribeLocalEvent<ADTPlantTrayUpgradeComponent, UpgradeExamineEvent>(OnTrayUpgradeExamine);
        SubscribeLocalEvent<ADTSeedExtractorUpgradeComponent, RefreshPartsEvent>(OnExtractorRefreshParts);
        SubscribeLocalEvent<ADTSeedExtractorUpgradeComponent, UpgradeExamineEvent>(OnExtractorUpgradeExamine);
    }

    private void OnTrayRefreshParts(EntityUid uid, ADTPlantTrayUpgradeComponent component, RefreshPartsEvent args)
    {
        var capacity = args.GetStatMultiplier(MachineStat.Capacity);

        component.WaterCapacityMultiplier = capacity;
        component.NutritionCapacityMultiplier = capacity;
        component.NutrientConsumptionMultiplier = args.GetStatMultiplier(MachineStat.ResourceCost);
        Dirty(uid, component);

        if (!TryComp<PlantTrayComponent>(uid, out var tray))
            return;

        component.BaseMaxWaterLevel ??= tray.MaxWaterLevel;
        component.BaseMaxNutritionLevel ??= tray.MaxNutritionLevel;
        tray.MaxWaterLevel = component.BaseMaxWaterLevel.Value * component.WaterCapacityMultiplier;
        tray.MaxNutritionLevel = component.BaseMaxNutritionLevel.Value * component.NutritionCapacityMultiplier;
        Dirty(uid, tray);
    }

    private static void OnTrayUpgradeExamine(EntityUid uid, ADTPlantTrayUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-hydro-water", component.WaterCapacityMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-hydro-nutrition", component.NutritionCapacityMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-hydro-nutrition-consume", component.NutrientConsumptionMultiplier, benefit: false);
    }

    private void OnExtractorRefreshParts(EntityUid uid, ADTSeedExtractorUpgradeComponent component, RefreshPartsEvent args)
    {
        component.SeedMultiplier = args.GetStatMultiplier(MachineStat.Speed);
        Dirty(uid, component);
    }

    private static void OnExtractorUpgradeExamine(EntityUid uid, ADTSeedExtractorUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-seed-extraction", component.SeedMultiplier, benefit: true);
    }

    public float GetNutrientConsumptionMultiplier(EntityUid tray)
    {
        return TryComp<ADTPlantTrayUpgradeComponent>(tray, out var upgrade) ? upgrade.NutrientConsumptionMultiplier : 1f;
    }

    public void ApplyTrayCycleDelay(EntityUid tray, EntityUid plant)
    {
        if (!TryComp<ADTPlantTrayUpgradeComponent>(tray, out var upgrade) || upgrade.CycleDelay is not { } delay)
            return;

        if (!TryComp<PlantHolderComponent>(plant, out var holder))
            return;

        holder.CycleDelay = delay;
        Dirty(plant, holder);
    }

    public float GetSeedMultiplier(EntityUid extractor)
    {
        return TryComp<ADTSeedExtractorUpgradeComponent>(extractor, out var upgrade) ? upgrade.SeedMultiplier : 1f;
    }
}
