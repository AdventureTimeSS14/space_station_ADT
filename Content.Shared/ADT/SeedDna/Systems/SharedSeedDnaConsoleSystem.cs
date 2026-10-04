using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ADT.SeedDna.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SeedDna.Systems;

[UsedImplicitly]
public abstract class SharedSeedDnaConsoleSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private BotanySystem _botany = default!;

    private const float MaxProduceVolume = 100f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedDnaConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SeedDnaConsoleComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, SeedDnaConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, SeedDnaConsoleComponent.SeedSlotId, component.SeedSlot);
        _itemSlotsSystem.AddItemSlot(uid, SeedDnaConsoleComponent.DnaDiskSlotId, component.DnaDiskSlot);
    }

    private void OnComponentRemove(EntityUid uid, SeedDnaConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.SeedSlot);
        _itemSlotsSystem.RemoveItemSlot(uid, component.DnaDiskSlot);
    }

    private bool TryGet<T>(EntityUid? snapshot, EntProtoId? protoId, [NotNullWhen(true)] out T? comp)
        where T : class, IComponent, new()
    {
        return _botany.TryGetPlantComponent(snapshot, protoId, out comp);
    }

    protected SeedDataDto? ExtractSeedData(EntityUid seed)
    {
        if (!TryComp<SeedComponent>(seed, out var seedComp))
            return null;

        var snapshot = seedComp.PlantData;
        EntProtoId? protoId = seedComp.PlantProtoId;

        if (!TryGet<PlantComponent>(snapshot, protoId, out var plant))
            return null;

        var growth = TryGet<PlantGrowthComponent>(snapshot, protoId, out var g) ? g : new PlantGrowthComponent();
        var atmos = TryGet<PlantAtmosphericComponent>(snapshot, protoId, out var a) ? a : new PlantAtmosphericComponent();
        var gases = TryGet<PlantConsumeExudeGasComponent>(snapshot, protoId, out var cg) ? cg : new PlantConsumeExudeGasComponent();
        var toxins = TryGet<PlantToxinsComponent>(snapshot, protoId, out var t) ? t : new PlantToxinsComponent();
        var weedPest = TryGet<PlantWeedPestComponent>(snapshot, protoId, out var w) ? w : new PlantWeedPestComponent();
        var harvest = TryGet<PlantHarvestComponent>(snapshot, protoId, out var h) ? h : new PlantHarvestComponent();

        var dto = new SeedDataDto
        {
            ConsumeGasses = new(gases.ConsumeGasses),
            ExudeGasses = new(gases.ExudeGasses),
            NutrientConsumption = growth.NutrientConsumption,
            WaterConsumption = growth.WaterConsumption,
            IdealHeat = (atmos.LowHeatTolerance + atmos.HighHeatTolerance) / 2f,
            HeatTolerance = (atmos.HighHeatTolerance - atmos.LowHeatTolerance) / 2f,
            ToxinsTolerance = toxins.ToxinsTolerance,
            LowPressureTolerance = atmos.LowPressureTolerance,
            HighPressureTolerance = atmos.HighPressureTolerance,
            PestTolerance = weedPest.PestTolerance,
            WeedTolerance = weedPest.WeedTolerance,
            Endurance = plant.Endurance,
            Yield = plant.Yield,
            Lifespan = plant.Lifespan,
            Maturation = plant.Maturation,
            Production = plant.Production,
            HarvestRepeat = (SharedHarvestTypeDto)(byte)harvest.HarvestRepeat,
            Potency = plant.Potency,
            Seedless = TryGet<PlantTraitSeedlessComponent>(snapshot, protoId, out _),
            Viable = !TryGet<PlantTraitUnviableComponent>(snapshot, protoId, out _),
            Ligneous = TryGet<PlantTraitLigneousComponent>(snapshot, protoId, out _),
            CanScream = TryGet<PlantTraitScreamComponent>(snapshot, protoId, out _),
            Chemicals = new Dictionary<string, SeedChemQuantityDto>(),
        };

        if (TryGet<PlantChemicalsComponent>(snapshot, protoId, out var chemicals))
        {
            foreach (var (key, value) in chemicals.Chemicals)
            {
                dto.Chemicals[key.Id] = new SeedChemQuantityDto
                {
                    Min = (int)value.Min,
                    Max = (int)value.Max,
                    PotencyDivisor = (int)value.PotencyDivisor,
                    Inherent = value.Inherent,
                };
            }
        }

        return dto;
    }

    protected void RewriteSeedData(EntityUid seed, SeedDataDto dto)
    {
        if (!TryComp<SeedComponent>(seed, out var seedComp))
            return;

        if (seedComp.PlantData == null)
        {
            var template = Spawn(seedComp.PlantProtoId, MapCoordinates.Nullspace);
            seedComp.PlantData = _botany.ClonePlantSnapshotData(template, parent: seed);
            Del(template);
            Dirty(seed, seedComp);
        }

        if (seedComp.PlantData is not { } snapshot)
            return;

        var plant = EnsureComp<PlantComponent>(snapshot);
        if (dto.Endurance != null)
            plant.Endurance = dto.Endurance.Value;
        if (dto.Yield != null)
            plant.Yield = dto.Yield.Value;
        if (dto.Lifespan != null)
            plant.Lifespan = dto.Lifespan.Value;
        if (dto.Maturation != null)
            plant.Maturation = dto.Maturation.Value;
        if (dto.Production != null)
            plant.Production = dto.Production.Value;
        if (dto.Potency != null)
            plant.Potency = dto.Potency.Value;
        Dirty(snapshot, plant);

        var growth = EnsureComp<PlantGrowthComponent>(snapshot);
        if (dto.NutrientConsumption != null)
            growth.NutrientConsumption = dto.NutrientConsumption.Value;
        if (dto.WaterConsumption != null)
            growth.WaterConsumption = dto.WaterConsumption.Value;
        Dirty(snapshot, growth);

        var atmos = EnsureComp<PlantAtmosphericComponent>(snapshot);
        if (dto.IdealHeat != null || dto.HeatTolerance != null)
        {
            var ideal = dto.IdealHeat ?? (atmos.LowHeatTolerance + atmos.HighHeatTolerance) / 2f;
            var tolerance = dto.HeatTolerance ?? (atmos.HighHeatTolerance - atmos.LowHeatTolerance) / 2f;
            atmos.LowHeatTolerance = ideal - tolerance;
            atmos.HighHeatTolerance = ideal + tolerance;
        }
        if (dto.LowPressureTolerance != null)
            atmos.LowPressureTolerance = dto.LowPressureTolerance.Value;
        if (dto.HighPressureTolerance != null)
            atmos.HighPressureTolerance = dto.HighPressureTolerance.Value;
        Dirty(snapshot, atmos);

        var gases = EnsureComp<PlantConsumeExudeGasComponent>(snapshot);
        if (dto.ConsumeGasses != null)
            gases.ConsumeGasses = new(dto.ConsumeGasses);
        if (dto.ExudeGasses != null)
            gases.ExudeGasses = new(dto.ExudeGasses);
        Dirty(snapshot, gases);

        var toxins = EnsureComp<PlantToxinsComponent>(snapshot);
        if (dto.ToxinsTolerance != null)
            toxins.ToxinsTolerance = dto.ToxinsTolerance.Value;
        Dirty(snapshot, toxins);

        var weedPest = EnsureComp<PlantWeedPestComponent>(snapshot);
        if (dto.PestTolerance != null)
            weedPest.PestTolerance = dto.PestTolerance.Value;
        if (dto.WeedTolerance != null)
            weedPest.WeedTolerance = dto.WeedTolerance.Value;
        Dirty(snapshot, weedPest);

        var harvest = EnsureComp<PlantHarvestComponent>(snapshot);
        if (dto.HarvestRepeat != null)
            harvest.HarvestRepeat = (HarvestType)(byte)dto.HarvestRepeat.Value;
        Dirty(snapshot, harvest);

        if (dto.Seedless != null)
            SetTrait<PlantTraitSeedlessComponent>(snapshot, dto.Seedless.Value);
        if (dto.Viable != null)
            SetTrait<PlantTraitUnviableComponent>(snapshot, !dto.Viable.Value);
        if (dto.Ligneous != null)
            SetTrait<PlantTraitLigneousComponent>(snapshot, dto.Ligneous.Value);
        if (dto.CanScream != null)
            SetTrait<PlantTraitScreamComponent>(snapshot, dto.CanScream.Value);

        if (dto.Chemicals != null)
            RewriteChemicals(snapshot, dto.Chemicals);
    }

    private void SetTrait<T>(EntityUid snapshot, bool enabled) where T : Component, new()
    {
        if (enabled)
            EnsureComp<T>(snapshot);
        else
            RemComp<T>(snapshot);
    }

    private void RewriteChemicals(EntityUid snapshot, Dictionary<string, SeedChemQuantityDto> chemicalsDto)
    {
        var chemicals = EnsureComp<PlantChemicalsComponent>(snapshot);
        chemicals.Chemicals.Clear();

        var currentVolume = 0f;
        foreach (var (key, value) in chemicalsDto)
        {
            float chemVolume = value.Max;

            // Remove the oldest chemicals to make room for the new one
            if (currentVolume + chemVolume > MaxProduceVolume)
            {
                var volumeNeeded = currentVolume + chemVolume - MaxProduceVolume;
                var chemicalKeys = chemicals.Chemicals.Keys.ToList();
                var keyIndex = 0;
                while (volumeNeeded > 0 && keyIndex < chemicalKeys.Count)
                {
                    var oldKey = chemicalKeys[keyIndex];
                    var chemMax = (float) chemicals.Chemicals[oldKey].Max;

                    currentVolume -= chemMax;
                    volumeNeeded -= chemMax;
                    chemicals.Chemicals.Remove(oldKey);

                    keyIndex++;
                }
            }

            chemicals.Chemicals[key] = new PlantChemQuantity
            {
                Min = value.Min,
                Max = value.Max,
                PotencyDivisor = value.PotencyDivisor,
                Inherent = value.Inherent,
            };
            currentVolume += chemVolume;
        }

        Dirty(snapshot, chemicals);
    }
}
