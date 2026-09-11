using Content.Server.Administration.Logs;
using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.Research.Systems;
using Content.Server.Station.Systems;
using Content.Shared.ADT.SeedDna;
using Content.Shared.ADT.SeedDna.Components;
using Content.Shared.ADT.SeedDna.Prototypes;
using Content.Shared.ADT.SeedDna.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Globalization;
using System.Linq;

namespace Content.Server.ADT.SeedDna.Systems;

[UsedImplicitly]
public sealed class SeedDnaConsoleSystem : SharedSeedDnaConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedResearchSystem _research = default!;
    [Dependency] private readonly ResearchSystem _researchServer = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedDnaConsoleComponent, SeedDnaGeneTransferMessage>(OnGeneTransfer);
        SubscribeLocalEvent<SeedDnaConsoleComponent, SeedDnaSellMessage>(OnSell);

        SubscribeLocalEvent<SeedDnaConsoleComponent, ComponentStartup>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntInsertedIntoContainerMessage>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntRemovedFromContainerMessage>(OnUpdateUserInterface);
    }

    private void OnMapInit(EntityUid uid, SeedDnaConsoleComponent component, MapInitEvent args)
    {
        component.Points = component.StartingPoints;
    }

    private void OnUpdateUserInterface(EntityUid uid, SeedDnaConsoleComponent component, EntityEventArgs args)
    {
        UpdateUserInterface(uid, component);
    }

    #region Transfer

    private void OnGeneTransfer(EntityUid uid, SeedDnaConsoleComponent component, SeedDnaGeneTransferMessage args)
    {
        if (!CanUseConsole(uid, component, args.Actor))
            return;

        if (_timing.CurTime < component.NextTransferAt)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-cooldown"), uid, args.Actor);
            return;
        }

        if (TryTransferGene(uid, component, args.GeneId, args.Direction))
            component.NextTransferAt = _timing.CurTime + component.TransferCooldown;

        UpdateUserInterface(uid, component);
    }

    private bool TryTransferGene(
        EntityUid uid,
        SeedDnaConsoleComponent component,
        string geneId,
        SeedDnaTransferDirection direction)
    {
        var gene = GetGene(component, geneId);
        if (gene == null)
            return false;

        if (!CanTransferGene(uid, gene))
            return false;

        var (seedItem, diskItem) = GetSlotItems(component);
        if (seedItem == null || diskItem == null)
            return false;

        var seedComponent = Comp<SeedComponent>(seedItem.Value);
        var diskComponent = Comp<DnaDiskComponent>(diskItem.Value);

        var sourceValue = direction == SeedDnaTransferDirection.SeedToDisk
            ? GetGeneValueFromSeed(seedComponent.Seed, geneId)
            : GetGeneValueFromDisk(diskComponent.SeedData, geneId);

        if (sourceValue == null)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-value"), uid);
            return false;
        }

        var targetValue = direction == SeedDnaTransferDirection.SeedToDisk
            ? GetGeneValueFromDisk(diskComponent.SeedData, geneId)
            : GetGeneValueFromSeed(seedComponent.Seed, geneId);

        if (!IsTransferUseful(gene, sourceValue, targetValue))
            return false;

        var cost = ComputeGeneCost(gene, sourceValue);
        if (cost > 0 && component.Points < cost)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-points"), uid);
            return false;
        }

        if (direction == SeedDnaTransferDirection.SeedToDisk)
        {
            diskComponent.SeedData ??= new SeedDataDto();
            if (!ApplyGeneToDisk(diskComponent.SeedData, geneId, sourceValue))
                return false;
            Dirty(diskItem.Value, diskComponent);
        }
        else
        {
            if (seedComponent.Seed?.Immutable == true)
            {
                _popup.PopupEntity(Loc.GetString("seed-dna-popup-immutable"), uid);
                return false;
            }

            if (!_botany.TryGetSeed(seedComponent, out var originalSeed))
                return false;

            var seedData = originalSeed.Clone();
            seedComponent.Seed = seedData;
            if (!ApplyGeneToSeed(seedData, geneId, sourceValue, component.MaxChemicalsVolume))
                return false;
            seedData.IsModified = true;
            Dirty(seedItem.Value, seedComponent);
        }

        if (cost > 0)
            component.Points -= cost;

        return true;
    }

    private (EntityUid?, EntityUid?) GetSlotItems(SeedDnaConsoleComponent component)
    {
        return (
            component.SeedSlot.Item is { Valid: true } seed ? seed : null,
            component.DnaDiskSlot.Item is { Valid: true } disk ? disk : null
        );
    }

    private SeedDnaGenePrototype? GetGene(SeedDnaConsoleComponent component, string geneId)
    {
        if (component.Genes.Contains(geneId))
            return _prototypes.TryIndex<SeedDnaGenePrototype>(geneId, out var staticGene) ? staticGene : null;

        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
            return GetSpecialGene(component, SeedDnaGeneType.Chemical);

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix) || geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
            return GetSpecialGene(component, SeedDnaGeneType.Gas);

        return null;
    }

    private SeedDnaGenePrototype? GetSpecialGene(SeedDnaConsoleComponent component, SeedDnaGeneType type)
    {
        foreach (var geneId in component.Genes)
        {
            if (_prototypes.TryIndex<SeedDnaGenePrototype>(geneId, out var gene) && gene.ValueType == type)
                return gene;
        }

        return null;
    }

    private static int ComputeGeneCost(SeedDnaGenePrototype gene, object? value)
    {
        if (gene.CostPerUnit > 0f && value != null)
            return Math.Max(1, (int)Math.Round(Convert.ToSingle(value) * gene.CostPerUnit));

        return gene.Cost;
    }

    private static bool IsTransferUseful(SeedDnaGenePrototype gene, object source, object? target)
    {
        if (target == null)
            return true;

        return gene.ValueType switch
        {
            SeedDnaGeneType.Float or SeedDnaGeneType.Int =>
                Convert.ToSingle(source) > Convert.ToSingle(target),
            SeedDnaGeneType.Bool => (bool)source && !(bool)target,
            _ => !source.Equals(target),
        };
    }

    private static bool HasMeaningfulValue(SeedDnaGeneType type, object? value)
    {
        if (value == null)
            return false;

        return type switch
        {
            SeedDnaGeneType.Bool => (bool)value,
            SeedDnaGeneType.HarvestType => (byte)value != (byte)HarvestType.NoRepeat,
            _ => true,
        };
    }

    private bool CanTransferGene(EntityUid uid, SeedDnaGenePrototype gene)
    {
        if (gene.RequiredTechnology is not { } technology)
            return true;

        if (!_researchServer.TryGetClientServer(uid, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-server"), uid);
            return false;
        }

        if (!_research.IsTechnologyUnlocked(uid, technology))
        {
            var techName = _prototypes.TryIndex<TechnologyPrototype>(technology, out var tech)
                ? Loc.GetString(tech.Name)
                : (string)technology;
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-tech-locked", ("tech", techName)), uid);
            return false;
        }

        return true;
    }

    private bool CanUseConsole(EntityUid uid, SeedDnaConsoleComponent component, EntityUid user)
    {
        if (!this.IsPowered(uid, EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-power"), uid, user);
            return false;
        }

        if (!_access.IsAllowed(user, uid))
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-access"), uid, user);
            return false;
        }

        return true;
    }

    #endregion

    #region Sell

    private void OnSell(EntityUid uid, SeedDnaConsoleComponent component, SeedDnaSellMessage args)
    {
        if (!CanUseConsole(uid, component, args.Actor))
            return;

        if (_timing.CurTime < component.NextSellAt)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-cooldown"), uid, args.Actor);
            return;
        }

        if (component.SeedSlot.Item is not { Valid: true } seedItem
            || Comp<SeedComponent>(seedItem).Seed is not { } seedData)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-seed"), uid, args.Actor);
            return;
        }

        if (component.RequireModifiedSeed && !seedData.IsModified)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-not-modified"), uid, args.Actor);
            UpdateUserInterface(uid, component);
            return;
        }

        if (_station.GetOwningStation(uid) is not { } station)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-station"), uid, args.Actor);
            return;
        }

        var price = ComputeSellPrice(component, seedData);
        if (price <= 0 || !_cargo.TryAdjustBankAccount(station, component.SellAccount, price))
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-station"), uid, args.Actor);
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor)} sold a plant genome for {price} credits on {ToPrettyString(uid)}");

        QueueDel(seedItem);
        component.NextSellAt = _timing.CurTime + component.SellCooldown;
        var pointsGain = (int)Math.Round(price * component.SellPointsPercent / 100f);
        component.Points = Math.Min(component.MaxPoints, component.Points + pointsGain);
        _popup.PopupEntity(Loc.GetString("seed-dna-popup-sold", ("price", price), ("points", pointsGain)), uid, args.Actor);
        UpdateUserInterface(uid, component);
    }

    private int ComputeSellPrice(SeedDnaConsoleComponent component, SeedData seed)
    {
        var price = component.SellBasePrice;

        foreach (var geneId in component.Genes)
        {
            if (!_prototypes.TryIndex<SeedDnaGenePrototype>(geneId, out var gene))
                continue;

            switch (gene.ValueType)
            {
                case SeedDnaGeneType.Float:
                    if (GetSeedGeneValue(seed, geneId) is float numeric)
                        price += (int)Math.Round(gene.SellPrice * Math.Clamp(numeric / Math.Max(gene.Max, 1f), 0f, 1f));
                    break;
                case SeedDnaGeneType.Int:
                    if (GetSeedGeneValue(seed, geneId) is int intValue)
                        price += (int)Math.Round(gene.SellPrice * Math.Clamp(intValue / Math.Max(gene.Max, 1f), 0f, 1f));
                    break;
                case SeedDnaGeneType.Bool:
                    if (GetSeedGeneValue(seed, geneId) is true)
                        price += gene.SellPrice;
                    break;
                case SeedDnaGeneType.HarvestType:
                    if (GetSeedGeneValue(seed, geneId) is byte harvest && harvest != (byte)HarvestType.NoRepeat)
                        price += gene.SellPrice;
                    break;
                case SeedDnaGeneType.Chemical:
                    price += seed.Chemicals.Count * component.SellChemicalPrice;
                    break;
                case SeedDnaGeneType.Gas:
                    price += (seed.ConsumeGasses.Count + seed.ExudeGasses.Count) * gene.SellPrice;
                    break;
            }
        }

        return price;
    }

    #endregion

    #region Gene values

    private object? GetGeneValueFromSeed(SeedData? seed, string geneId)
    {
        if (seed == null)
            return null;

        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            var reagentId = geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..];
            return seed.Chemicals.TryGetValue(reagentId, out var quantity)
                ? ToDto(quantity)
                : null;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                return null;
            return seed.ConsumeGasses.TryGetValue(gas, out var value) ? value : null;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                return null;
            return seed.ExudeGasses.TryGetValue(gas, out var value) ? value : null;
        }

        return GetSeedGeneValue(seed, geneId);
    }

    private object? GetGeneValueFromDisk(SeedDataDto? diskData, string geneId)
    {
        if (diskData == null)
            return null;

        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            var reagentId = geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..];
            return diskData.Chemicals != null && diskData.Chemicals.TryGetValue(reagentId, out var quantity)
                ? quantity
                : null;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                return null;
            return diskData.ConsumeGasses != null && diskData.ConsumeGasses.TryGetValue(gas, out var value) ? value : null;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                return null;
            return diskData.ExudeGasses != null && diskData.ExudeGasses.TryGetValue(gas, out var value) ? value : null;
        }

        return GetDtoGeneValue(diskData, geneId);
    }

    private bool ApplyGeneToSeed(SeedData seed, string geneId, object value, float maxChemicalsVolume)
    {
        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            AddChemical(seed, geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..], (SeedChemQuantityDto)value, maxChemicalsVolume);
            return true;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                return false;
            seed.ConsumeGasses[gas] = (float)value;
            return true;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                return false;
            seed.ExudeGasses[gas] = (float)value;
            return true;
        }

        SetSeedGeneValue(seed, geneId, value);
        return true;
    }

    private bool ApplyGeneToDisk(SeedDataDto diskData, string geneId, object value)
    {
        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            diskData.Chemicals ??= new Dictionary<string, SeedChemQuantityDto>();
            diskData.Chemicals[geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..]] = (SeedChemQuantityDto)value;
            return true;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                return false;
            diskData.ConsumeGasses ??= new Dictionary<Gas, float>();
            diskData.ConsumeGasses[gas] = (float)value;
            return true;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                return false;
            diskData.ExudeGasses ??= new Dictionary<Gas, float>();
            diskData.ExudeGasses[gas] = (float)value;
            return true;
        }

        SetDtoGeneValue(diskData, geneId, value);
        return true;
    }

    private static void AddChemical(SeedData seed, string reagentId, SeedChemQuantityDto quantity, float maxVolume)
    {
        var currentVolume = seed.Chemicals.Values.Sum(chem => chem.Max.Float());
        if (seed.Chemicals.TryGetValue(reagentId, out var existing))
            currentVolume -= existing.Max.Float();

        while (currentVolume + quantity.Max > maxVolume)
        {
            var victim = seed.Chemicals
                .Where(pair => pair.Key != reagentId)
                .MinBy(pair => pair.Value.Max.Float());

            if (victim.Key == null)
                break;

            currentVolume -= victim.Value.Max.Float();
            seed.Chemicals.Remove(victim.Key);
        }

        seed.Chemicals[reagentId] = new SeedChemQuantity
        {
            Min = quantity.Min,
            Max = quantity.Max,
            PotencyDivisor = quantity.PotencyDivisor,
            Inherent = quantity.Inherent,
        };
    }

    private static SeedChemQuantityDto ToDto(SeedChemQuantity quantity)
    {
        return new SeedChemQuantityDto
        {
            Min = quantity.Min.Float(),
            Max = quantity.Max.Float(),
            PotencyDivisor = quantity.PotencyDivisor,
            Inherent = quantity.Inherent,
        };
    }

    private static object? GetSeedGeneValue(SeedData seed, string geneId) => geneId switch
    {
        "NutrientConsumption" => seed.NutrientConsumption,
        "WaterConsumption" => seed.WaterConsumption,
        "IdealHeat" => seed.IdealHeat,
        "HeatTolerance" => seed.HeatTolerance,
        "ToxinsTolerance" => seed.ToxinsTolerance,
        "LowPressureTolerance" => seed.LowPressureTolerance,
        "HighPressureTolerance" => seed.HighPressureTolerance,
        "PestTolerance" => seed.PestTolerance,
        "WeedTolerance" => seed.WeedTolerance,
        "Endurance" => seed.Endurance,
        "Yield" => seed.Yield,
        "Lifespan" => seed.Lifespan,
        "Maturation" => seed.Maturation,
        "Production" => seed.Production,
        "HarvestRepeat" => (byte)seed.HarvestRepeat,
        "Potency" => seed.Potency,
        "Seedless" => seed.Seedless,
        "Viable" => seed.Viable,
        "Ligneous" => seed.Ligneous,
        "CanScream" => seed.CanScream,
        _ => null,
    };

    private static object? GetDtoGeneValue(SeedDataDto dto, string geneId) => geneId switch
    {
        "NutrientConsumption" => dto.NutrientConsumption,
        "WaterConsumption" => dto.WaterConsumption,
        "IdealHeat" => dto.IdealHeat,
        "HeatTolerance" => dto.HeatTolerance,
        "ToxinsTolerance" => dto.ToxinsTolerance,
        "LowPressureTolerance" => dto.LowPressureTolerance,
        "HighPressureTolerance" => dto.HighPressureTolerance,
        "PestTolerance" => dto.PestTolerance,
        "WeedTolerance" => dto.WeedTolerance,
        "Endurance" => dto.Endurance,
        "Yield" => dto.Yield,
        "Lifespan" => dto.Lifespan,
        "Maturation" => dto.Maturation,
        "Production" => dto.Production,
        "HarvestRepeat" => dto.HarvestRepeat is { } harvest ? (byte)harvest : null,
        "Potency" => dto.Potency,
        "Seedless" => dto.Seedless,
        "Viable" => dto.Viable,
        "Ligneous" => dto.Ligneous,
        "CanScream" => dto.CanScream,
        _ => null,
    };

    private static void SetSeedGeneValue(SeedData seed, string geneId, object value)
    {
        switch (geneId)
        {
            case "NutrientConsumption": seed.NutrientConsumption = Convert.ToSingle(value); break;
            case "WaterConsumption": seed.WaterConsumption = Convert.ToSingle(value); break;
            case "IdealHeat": seed.IdealHeat = Convert.ToSingle(value); break;
            case "HeatTolerance": seed.HeatTolerance = Convert.ToSingle(value); break;
            case "ToxinsTolerance": seed.ToxinsTolerance = Convert.ToSingle(value); break;
            case "LowPressureTolerance": seed.LowPressureTolerance = Convert.ToSingle(value); break;
            case "HighPressureTolerance": seed.HighPressureTolerance = Convert.ToSingle(value); break;
            case "PestTolerance": seed.PestTolerance = Convert.ToSingle(value); break;
            case "WeedTolerance": seed.WeedTolerance = Convert.ToSingle(value); break;
            case "Endurance": seed.Endurance = Convert.ToSingle(value); break;
            case "Yield": seed.Yield = (int)Math.Round(Convert.ToSingle(value)); break;
            case "Lifespan": seed.Lifespan = Convert.ToSingle(value); break;
            case "Maturation": seed.Maturation = Convert.ToSingle(value); break;
            case "Production": seed.Production = Convert.ToSingle(value); break;
            case "HarvestRepeat": seed.HarvestRepeat = (HarvestType)Convert.ToByte(value); break;
            case "Potency": seed.Potency = Convert.ToSingle(value); break;
            case "Seedless": seed.Seedless = Convert.ToBoolean(value); break;
            case "Viable": seed.Viable = Convert.ToBoolean(value); break;
            case "Ligneous": seed.Ligneous = Convert.ToBoolean(value); break;
            case "CanScream": seed.CanScream = Convert.ToBoolean(value); break;
        }
    }

    private static void SetDtoGeneValue(SeedDataDto dto, string geneId, object value)
    {
        switch (geneId)
        {
            case "NutrientConsumption": dto.NutrientConsumption = Convert.ToSingle(value); break;
            case "WaterConsumption": dto.WaterConsumption = Convert.ToSingle(value); break;
            case "IdealHeat": dto.IdealHeat = Convert.ToSingle(value); break;
            case "HeatTolerance": dto.HeatTolerance = Convert.ToSingle(value); break;
            case "ToxinsTolerance": dto.ToxinsTolerance = Convert.ToSingle(value); break;
            case "LowPressureTolerance": dto.LowPressureTolerance = Convert.ToSingle(value); break;
            case "HighPressureTolerance": dto.HighPressureTolerance = Convert.ToSingle(value); break;
            case "PestTolerance": dto.PestTolerance = Convert.ToSingle(value); break;
            case "WeedTolerance": dto.WeedTolerance = Convert.ToSingle(value); break;
            case "Endurance": dto.Endurance = Convert.ToSingle(value); break;
            case "Yield": dto.Yield = (int)Math.Round(Convert.ToSingle(value)); break;
            case "Lifespan": dto.Lifespan = Convert.ToSingle(value); break;
            case "Maturation": dto.Maturation = Convert.ToSingle(value); break;
            case "Production": dto.Production = Convert.ToSingle(value); break;
            case "HarvestRepeat": dto.HarvestRepeat = (SharedHarvestTypeDto)Convert.ToByte(value); break;
            case "Potency": dto.Potency = Convert.ToSingle(value); break;
            case "Seedless": dto.Seedless = Convert.ToBoolean(value); break;
            case "Viable": dto.Viable = Convert.ToBoolean(value); break;
            case "Ligneous": dto.Ligneous = Convert.ToBoolean(value); break;
            case "CanScream": dto.CanScream = Convert.ToBoolean(value); break;
        }
    }

    #endregion

    #region User interface

    private void UpdateUserInterface(EntityUid uid, SeedDnaConsoleComponent component)
    {
        if (!component.Initialized)
            return;

        var (seedPresent, seedName, seedData) = ProcessSeedSlot(component);
        var (diskPresent, diskName, diskData) = ProcessDiskSlot(component);

        var connected = _researchServer.TryGetClientServer(uid, out _, out _);
        var canSell = seedData != null && _timing.CurTime >= component.NextSellAt;

        var newState = new SeedDnaConsoleBoundUserInterfaceState(
            seedPresent,
            seedName,
            diskPresent,
            diskName,
            BuildGeneEntries(uid, component, seedData, diskData),
            connected,
            seedData != null ? ComputeSellPrice(component, seedData) : 0,
            canSell,
            component.Points,
            component.MaxPoints,
            seedData != null && (!component.RequireModifiedSeed || seedData.IsModified)
        );
        _userInterface.SetUiState(uid, SeedDnaConsoleUiKey.Key, newState);
    }

    private List<SeedDnaGeneEntry> BuildGeneEntries(
        EntityUid uid,
        SeedDnaConsoleComponent component,
        SeedData? seedData,
        SeedDataDto? diskData)
    {
        var entries = new List<SeedDnaGeneEntry>();

        foreach (var geneId in component.Genes)
        {
            if (!_prototypes.TryIndex<SeedDnaGenePrototype>(geneId, out var gene))
                continue;

            if (gene.ValueType == SeedDnaGeneType.Chemical)
            {
                AddChemicalEntries(entries, uid, component, gene, seedData, diskData);
                continue;
            }

            if (gene.ValueType == SeedDnaGeneType.Gas)
            {
                AddGasEntries(entries, uid, component, gene, seedData, diskData);
                continue;
            }

            var rawSeed = seedData != null ? GetSeedGeneValue(seedData, geneId) : null;
            var rawDisk = diskData != null ? GetDtoGeneValue(diskData, geneId) : null;
            if (!HasMeaningfulValue(gene.ValueType, rawSeed) && !HasMeaningfulValue(gene.ValueType, rawDisk))
                continue;

            var lockedTech = GetLockedTech(uid, gene);

            entries.Add(new SeedDnaGeneEntry
            {
                Id = geneId,
                Name = $"seed-dna-row-{geneId}",
                Type = gene.ValueType,
                SeedValue = FormatGeneValue(gene.ValueType, () => seedData != null ? GetSeedGeneValue(seedData, geneId) : null),
                DiskValue = FormatGeneValue(gene.ValueType, () => diskData != null ? GetDtoGeneValue(diskData, geneId) : null),
                Locked = lockedTech != null,
                Cost = ComputeGeneCost(gene, rawSeed),
                LockedTech = lockedTech,
                Description = gene.Description,
            });
        }

        return entries;
    }

    private void AddChemicalEntries(
        List<SeedDnaGeneEntry> entries,
        EntityUid uid,
        SeedDnaConsoleComponent component,
        SeedDnaGenePrototype gene,
        SeedData? seedData,
        SeedDataDto? diskData)
    {
        var chemicals = new HashSet<string>();
        if (seedData != null)
            chemicals.UnionWith(seedData.Chemicals.Keys);
        if (diskData?.Chemicals != null)
            chemicals.UnionWith(diskData.Chemicals.Keys);

        foreach (var reagentId in chemicals)
        {
            var seedPotency = seedData?.Potency;
            var diskPotency = diskData?.Potency ?? seedData?.Potency;

            SeedChemQuantityDto? seedChem = null;
            if (seedData != null && seedData.Chemicals.TryGetValue(reagentId, out var seedQuantity))
                seedChem = ToDto(seedQuantity);

            var lockedTech = GetLockedTech(uid, gene);

            entries.Add(new SeedDnaGeneEntry
            {
                Id = $"chemical:{reagentId}",
                Name = $"seed-dna-chemical-{reagentId}",
                Type = SeedDnaGeneType.Chemical,
                SeedValue = FormatChemical(seedChem, seedPotency),
                DiskValue = FormatChemical(diskData?.Chemicals?.GetValueOrDefault(reagentId), diskPotency),
                Locked = lockedTech != null,
                Cost = gene.Cost,
                LockedTech = lockedTech,
            });
        }
    }

    private void AddGasEntries(
        List<SeedDnaGeneEntry> entries,
        EntityUid uid,
        SeedDnaConsoleComponent component,
        SeedDnaGenePrototype gene,
        SeedData? seedData,
        SeedDataDto? diskData)
    {
        if ((seedData?.ConsumeGasses.Count ?? 0) + (seedData?.ExudeGasses.Count ?? 0)
            + (diskData?.ConsumeGasses?.Count ?? 0) + (diskData?.ExudeGasses?.Count ?? 0) == 0)
            return;

        var gasses = new HashSet<Gas>();
        if (seedData != null)
        {
            gasses.UnionWith(seedData.ConsumeGasses.Keys);
            gasses.UnionWith(seedData.ExudeGasses.Keys);
        }
        if (diskData?.ConsumeGasses != null)
            gasses.UnionWith(diskData.ConsumeGasses.Keys);
        if (diskData?.ExudeGasses != null)
            gasses.UnionWith(diskData.ExudeGasses.Keys);

        foreach (var gas in gasses)
        {
            var lockedTech = GetLockedTech(uid, gene);

            entries.Add(new SeedDnaGeneEntry
            {
                Id = $"consumeGas:{gas}",
                Name = "seed-dna-row-consume-gas",
                GasName = gas.ToString(),
                Type = SeedDnaGeneType.Gas,
                SeedValue = FormatFloat(seedData?.ConsumeGasses.GetValueOrDefault(gas)),
                DiskValue = FormatFloat(diskData?.ConsumeGasses?.GetValueOrDefault(gas)),
                Locked = lockedTech != null,
                Cost = gene.Cost,
                LockedTech = lockedTech,
            });

            entries.Add(new SeedDnaGeneEntry
            {
                Id = $"exudeGas:{gas}",
                Name = "seed-dna-row-exude-gas",
                GasName = gas.ToString(),
                Type = SeedDnaGeneType.Gas,
                SeedValue = FormatFloat(seedData?.ExudeGasses.GetValueOrDefault(gas)),
                DiskValue = FormatFloat(diskData?.ExudeGasses?.GetValueOrDefault(gas)),
                Locked = lockedTech != null,
                Cost = gene.Cost,
                LockedTech = lockedTech,
            });
        }
    }

    private string? GetLockedTech(EntityUid uid, SeedDnaGenePrototype gene)
    {
        if (gene.RequiredTechnology is not { } technology)
            return null;

        if (_researchServer.TryGetClientServer(uid, out _, out _) && _research.IsTechnologyUnlocked(uid, technology))
            return null;

        return _prototypes.TryIndex<TechnologyPrototype>(technology, out var tech)
            ? Loc.GetString(tech.Name)
            : (string)technology;
    }

    private string? FormatGeneValue(SeedDnaGeneType type, Func<object?> getValue)
    {
        return type switch
        {
            SeedDnaGeneType.Float => FormatFloat(getValue() as float?),
            SeedDnaGeneType.Int => getValue()?.ToString(),
            SeedDnaGeneType.Bool => getValue() is bool boolean
                ? Loc.GetString(boolean ? "seed-dna-bool-true" : "seed-dna-bool-false")
                : null,
            SeedDnaGeneType.HarvestType => getValue() is byte harvest
                ? Loc.GetString($"seed-dna-harvest-{Enum.GetName((HarvestType)harvest)}")
                : null,
            _ => null,
        };
    }

    private static string? FormatChemical(SeedChemQuantityDto? quantity, float? potency)
    {
        if (quantity is not { } chem || potency is not { } pot || chem.PotencyDivisor <= 0)
            return null;

        var amount = Math.Clamp(chem.Min + pot / chem.PotencyDivisor, chem.Min, chem.Max);
        return $"{amount.ToString("0.##", CultureInfo.InvariantCulture)}u";
    }

    private static string? FormatFloat(float? value)
    {
        return value?.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private (bool, string, SeedData?) ProcessSeedSlot(SeedDnaConsoleComponent component)
    {
        return component.SeedSlot.Item is not { Valid: true } seedItem
            ? (false, string.Empty, null)
            : (true, Name(seedItem), Comp<SeedComponent>(seedItem).Seed);
    }

    private (bool, string, SeedDataDto?) ProcessDiskSlot(SeedDnaConsoleComponent component)
    {
        return component.DnaDiskSlot.Item is not { Valid: true } diskItem
            ? (false, string.Empty, null)
            : (true, Name(diskItem), Comp<DnaDiskComponent>(diskItem).SeedData);
    }

    #endregion
}
