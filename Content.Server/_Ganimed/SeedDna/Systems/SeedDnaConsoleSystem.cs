using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared._Ganimed.SeedDna;
using Content.Shared._Ganimed.SeedDna.Components;
using Content.Shared._Ganimed.SeedDna.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server._Ganimed.SeedDna.Systems;

[UsedImplicitly]
public sealed class SeedDnaConsoleSystem : SharedSeedDnaConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedDnaConsoleComponent, WriteToTargetSeedDataMessage>(OnWriteToTargetSeedDataMessage);

        SubscribeLocalEvent<SeedDnaConsoleComponent, ComponentStartup>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntInsertedIntoContainerMessage>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntRemovedFromContainerMessage>(OnUpdateUserInterface);
    }

    private void OnUpdateUserInterface(EntityUid uid, SeedDnaConsoleComponent component, EntityEventArgs args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnWriteToTargetSeedDataMessage(EntityUid uid, SeedDnaConsoleComponent component, WriteToTargetSeedDataMessage args)
    {
        if (args.Target == TargetSeedData.Seed && component.SeedSlot.Item is { Valid: true } seedItem)
            RewriteSeedData(seedItem, args.SeedDataDto);
        else if (args.Target == TargetSeedData.DnaDisk && component.DnaDiskSlot.Item is { Valid: true } dnaDiskItem)
            RewriteDnaDiskData(dnaDiskItem, args.SeedDataDto);

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, SeedDnaConsoleComponent component)
    {
        if (!component.Initialized)
            return;

        var (seedPresent, seedName, seedData) = ProcessSeedSlot(component);
        var (dnaDiskPresent, dnaDiskName, dnaDiskData) = ProcessDiskSlot(component);

        var newState = new SeedDnaConsoleBoundUserInterfaceState(
            seedPresent,
            seedName,
            seedData,
            dnaDiskPresent,
            dnaDiskName,
            dnaDiskData
        );
        _userInterface.SetUiState(uid, SeedDnaConsoleUiKey.Key, newState);
    }

    private (bool, string, SeedDataDto?) ProcessSeedSlot(SeedDnaConsoleComponent component)
    {
        return component.SeedSlot.Item is not { Valid: true } seedItem
            ? (false, string.Empty, null)
            : (true, EntityManager.GetComponent<MetaDataComponent>(seedItem).EntityName, ExtractSeedData(seedItem));
    }

    private void RewriteSeedData(EntityUid seed, SeedDataDto seedDataDto)
    {
        var seedComponent = EntityManager.GetComponent<SeedComponent>(seed);
        var originalSeedData = seedComponent.Seed;

        // Clone the original seed to preserve appearance data like PlantRsi
        var seedData = originalSeedData?.Clone() ?? new SeedData();
        seedComponent.Seed = seedData;

        //@formatter:off
        if (seedDataDto.ConsumeGasses != null) seedData.ConsumeGasses = seedDataDto.ConsumeGasses;
        if (seedDataDto.ExudeGasses != null) seedData.ExudeGasses = seedDataDto.ExudeGasses;
        if (seedDataDto.NutrientConsumption != null) seedData.NutrientConsumption = seedDataDto.NutrientConsumption.Value;
        if (seedDataDto.WaterConsumption != null) seedData.WaterConsumption = seedDataDto.WaterConsumption.Value;
        if (seedDataDto.IdealHeat != null) seedData.IdealHeat = seedDataDto.IdealHeat.Value;
        if (seedDataDto.HeatTolerance != null) seedData.HeatTolerance = seedDataDto.HeatTolerance.Value;
        if (seedDataDto.ToxinsTolerance != null) seedData.ToxinsTolerance = seedDataDto.ToxinsTolerance.Value;
        if (seedDataDto.LowPressureTolerance != null) seedData.LowPressureTolerance = seedDataDto.LowPressureTolerance.Value;
        if (seedDataDto.HighPressureTolerance != null) seedData.HighPressureTolerance = seedDataDto.HighPressureTolerance.Value;
        if (seedDataDto.PestTolerance != null) seedData.PestTolerance = seedDataDto.PestTolerance.Value;
        if (seedDataDto.WeedTolerance != null) seedData.WeedTolerance = seedDataDto.WeedTolerance.Value;
        if (seedDataDto.Endurance != null) seedData.Endurance = seedDataDto.Endurance.Value;
        if (seedDataDto.Yield != null) seedData.Yield = seedDataDto.Yield.Value;
        if (seedDataDto.Lifespan != null) seedData.Lifespan = seedDataDto.Lifespan.Value;
        if (seedDataDto.Maturation != null) seedData.Maturation = seedDataDto.Maturation.Value;
        if (seedDataDto.Production != null) seedData.Production = seedDataDto.Production.Value;
        if (seedDataDto.HarvestRepeat != null) seedData.HarvestRepeat = (HarvestType)(byte)seedDataDto.HarvestRepeat.Value;
        if (seedDataDto.Potency != null) seedData.Potency = seedDataDto.Potency.Value;
        if (seedDataDto.Seedless != null) seedData.Seedless = seedDataDto.Seedless.Value;
        if (seedDataDto.Viable != null) seedData.Viable = seedDataDto.Viable.Value;
        if (seedDataDto.Ligneous != null) seedData.Ligneous = seedDataDto.Ligneous.Value;
        if (seedDataDto.CanScream != null) seedData.CanScream = seedDataDto.CanScream.Value;
        //@formatter:on

        if (seedDataDto.Chemicals != null)
        {
            // Clear old chemicals first
            seedData.Chemicals.Clear();

            // Add new chemicals in order, removing older ones if total exceeds 100u
            const float MaxProduceVolume = 100f;
            float currentVolume = 0f;

            foreach (var (key, value) in seedDataDto.Chemicals)
            {
                var seedChemQuantity = new SeedChemQuantity
                {
                    Min = value.Min,
                    Max = value.Max,
                    PotencyDivisor = value.PotencyDivisor,
                    Inherent = value.Inherent,
                };

                float chemVolume = value.Max;

                // Check if adding this chemical would exceed the limit
                if (currentVolume + chemVolume > MaxProduceVolume)
                {
                    // Calculate how much volume needs to be freed
                    float volumeNeeded = currentVolume + chemVolume - MaxProduceVolume;

                    // Remove the oldest chemicals to make room for the new one
                    var chemicalKeys = seedData.Chemicals.Keys.ToList();
                    int keyIndex = 0;
                    while (volumeNeeded > 0 && keyIndex < chemicalKeys.Count)
                    {
                        var oldKey = chemicalKeys[keyIndex];
                        var oldChem = seedData.Chemicals[oldKey];
                        float chemMax = oldChem.Max;

                        currentVolume -= chemMax;
                        volumeNeeded -= chemMax;
                        seedData.Chemicals.Remove(oldKey);

                        keyIndex++;
                    }
                }

                seedData.Chemicals[key] = seedChemQuantity;
                currentVolume += chemVolume;
            }
        }
    }

    private void RewriteDnaDiskData(EntityUid dnaDisk, SeedDataDto dnaDiskDataDto)
    {
        EntityManager.GetComponent<DnaDiskComponent>(dnaDisk).SeedData = dnaDiskDataDto;
    }

    private SeedDataDto? ExtractSeedData(EntityUid seed)
    {
        var seedData = EntityManager.GetComponent<SeedComponent>(seed).Seed;
        if (seedData == null)
            return null;

        var seedDataDto = new SeedDataDto
        {
            ConsumeGasses = seedData.ConsumeGasses,
            ExudeGasses = seedData.ExudeGasses,
            NutrientConsumption = seedData.NutrientConsumption,
            WaterConsumption = seedData.WaterConsumption,
            IdealHeat = seedData.IdealHeat,
            HeatTolerance = seedData.HeatTolerance,
            ToxinsTolerance = seedData.ToxinsTolerance,
            LowPressureTolerance = seedData.LowPressureTolerance,
            HighPressureTolerance = seedData.HighPressureTolerance,
            PestTolerance = seedData.PestTolerance,
            WeedTolerance = seedData.WeedTolerance,
            Endurance = seedData.Endurance,
            Yield = seedData.Yield,
            Lifespan = seedData.Lifespan,
            Maturation = seedData.Maturation,
            Production = seedData.Production,
            HarvestRepeat = (SharedHarvestTypeDto)(byte)seedData.HarvestRepeat,
            Potency = seedData.Potency,
            Seedless = seedData.Seedless,
            Viable = seedData.Viable,
            Ligneous = seedData.Ligneous,
            CanScream = seedData.CanScream,
        };

        seedDataDto.Chemicals = new Dictionary<string, SeedChemQuantityDto>();
        foreach (var (key, value) in seedData.Chemicals)
        {
            var dto = new SeedChemQuantityDto
            {
                Min = value.Min,
                Max = value.Max,
                PotencyDivisor = value.PotencyDivisor,
                Inherent = value.Inherent,
            };
            seedDataDto.Chemicals[key] = dto;
        }

        return seedDataDto;
    }

    private (bool, string, SeedDataDto?) ProcessDiskSlot(SeedDnaConsoleComponent component)
    {
        return component.DnaDiskSlot.Item is not { Valid: true } diskItem
            ? (false, string.Empty, null)
            : (true, EntityManager.GetComponent<MetaDataComponent>(diskItem).EntityName, ExtractDiskData(diskItem));
    }

    private SeedDataDto? ExtractDiskData(EntityUid dnaDisk)
    {
        return EntityManager.GetComponent<DnaDiskComponent>(dnaDisk).SeedData;
    }
}
