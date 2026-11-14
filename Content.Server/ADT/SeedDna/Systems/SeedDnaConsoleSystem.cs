using Content.Server.Administration.Logs;
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
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedResearchSystem _research = default!;
    [Dependency] private readonly ResearchSystem _researchServer = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private Dictionary<string, ProtoId<TechnologyPrototype>>? _geneTechnologies;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedDnaConsoleComponent, SeedDnaGeneTransferMessage>(OnGeneTransfer);
        SubscribeLocalEvent<SeedDnaConsoleComponent, SeedDnaSellMessage>(OnSell);

        SubscribeLocalEvent<SeedDnaConsoleComponent, ComponentStartup>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntInsertedIntoContainerMessage>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntRemovedFromContainerMessage>(OnUpdateUserInterface);
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

        var diskComponent = Comp<DnaDiskComponent>(diskItem.Value);
        var seedData = ExtractSeedData(seedItem.Value);

        var sourceValue = direction == SeedDnaTransferDirection.SeedToDisk
            ? GetGeneValue(seedData, geneId)
            : GetGeneValue(diskComponent.SeedData, geneId);

        if (sourceValue == null)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-value"), uid);
            return false;
        }

        var targetValue = direction == SeedDnaTransferDirection.SeedToDisk
            ? GetGeneValue(diskComponent.SeedData, geneId)
            : GetGeneValue(seedData, geneId);

        var isPurification = direction == SeedDnaTransferDirection.SeedToDisk
            && gene.ValueType is SeedDnaGeneType.Chemical or SeedDnaGeneType.Gas;
        if (!isPurification && !IsTransferUseful(gene, sourceValue, targetValue))
            return false;

        var instabilityCost = GetInstabilityCost(component, gene, geneId);

        if (direction == SeedDnaTransferDirection.SeedToDisk)
        {
            diskComponent.SeedData ??= new SeedDataDto();
            if (!ApplyGeneToDto(diskComponent.SeedData, geneId, sourceValue))
                return false;

            if (seedData != null)
            {
                CutGeneFromDto(seedData, gene, geneId);
                RewriteSeedData(seedItem.Value, seedData);
                AddSeedInstability(seedItem.Value, instabilityCost);
            }

            Dirty(diskItem.Value, diskComponent);
        }
        else
        {
            if (seedData == null)
                return false;

            if (!ApplyGeneToDto(seedData, geneId, sourceValue, component.MaxChemicalsVolume))
                return false;

            RewriteSeedData(seedItem.Value, seedData);
            AddSeedInstability(seedItem.Value, instabilityCost);
        }

        return true;
    }

    private static float GetInstabilityCost(
        SeedDnaConsoleComponent component,
        SeedDnaGenePrototype gene,
        string geneId)
    {
        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
            return component.InstabilityPerChemical;
        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix)
            || geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
            return component.InstabilityPerGas;

        return gene.InstabilityCost;
    }

    private static void CutGeneFromDto(SeedDataDto seedData, SeedDnaGenePrototype gene, string geneId)
    {
        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            seedData.Chemicals?.Remove(geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..]);
            return;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                seedData.ConsumeGasses?.Remove(gas);
            return;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                seedData.ExudeGasses?.Remove(gas);
            return;
        }

        switch (geneId)
        {
            case "HarvestRepeat":
                seedData.HarvestRepeat = SharedHarvestTypeDto.NoRepeat;
                break;
            case "Seedless":
                seedData.Seedless = false;
                break;
            case "Viable":
                seedData.Viable = false;
                break;
            case "Ligneous":
                seedData.Ligneous = false;
                break;
            case "CanScream":
                seedData.CanScream = false;
                break;
            case "Yield":
                seedData.Yield = (int)gene.Min;
                break;
            default:
                if (gene.ValueType == SeedDnaGeneType.Float)
                    SetDtoGeneValue(seedData, geneId, gene.Min);
                break;
        }
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
            SeedDnaGeneType.HarvestType => (byte)value != (byte)SharedHarvestTypeDto.NoRepeat,
            _ => true,
        };
    }

    private bool CanTransferGene(EntityUid uid, SeedDnaGenePrototype gene)
    {
        if (!TryGetGeneTechnology(gene.ID, out var technology))
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
            || ExtractSeedData(seedItem) is not { } seedData)
        {
            _popup.PopupEntity(Loc.GetString("seed-dna-popup-no-seed"), uid, args.Actor);
            return;
        }

        if (component.RequireModifiedSeed && !IsSeedModified(seedItem))
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
        _popup.PopupEntity(Loc.GetString("seed-dna-popup-sold", ("price", price)), uid, args.Actor);
        UpdateUserInterface(uid, component);
    }

    private int ComputeSellPrice(SeedDnaConsoleComponent component, SeedDataDto seed)
    {
        var price = component.SellBasePrice;

        foreach (var geneId in component.Genes)
        {
            if (!_prototypes.TryIndex<SeedDnaGenePrototype>(geneId, out var gene))
                continue;

            switch (gene.ValueType)
            {
                case SeedDnaGeneType.Float:
                    if (GetDtoGeneValue(seed, geneId) is float numeric)
                        price += (int)Math.Round(gene.SellPrice * Math.Clamp(numeric / Math.Max(gene.Max, 1f), 0f, 1f));
                    break;
                case SeedDnaGeneType.Int:
                    if (GetDtoGeneValue(seed, geneId) is int intValue)
                        price += (int)Math.Round(gene.SellPrice * Math.Clamp(intValue / Math.Max(gene.Max, 1f), 0f, 1f));
                    break;
                case SeedDnaGeneType.Bool:
                    if (GetDtoGeneValue(seed, geneId) is true)
                        price += gene.SellPrice;
                    break;
                case SeedDnaGeneType.HarvestType:
                    if (GetDtoGeneValue(seed, geneId) is byte harvest && harvest != (byte)SharedHarvestTypeDto.NoRepeat)
                        price += gene.SellPrice;
                    break;
                case SeedDnaGeneType.Chemical:
                    price += (seed.Chemicals?.Count ?? 0) * component.SellChemicalPrice;
                    break;
                case SeedDnaGeneType.Gas:
                    price += ((seed.ConsumeGasses?.Count ?? 0) + (seed.ExudeGasses?.Count ?? 0)) * gene.SellPrice;
                    break;
            }
        }

        return price;
    }

    #endregion

    #region Gene values

    private object? GetGeneValue(SeedDataDto? seedData, string geneId)
    {
        if (seedData == null)
            return null;

        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            var reagentId = geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..];
            return seedData.Chemicals != null && seedData.Chemicals.TryGetValue(reagentId, out var quantity)
                ? quantity
                : null;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                return null;
            return seedData.ConsumeGasses != null && seedData.ConsumeGasses.TryGetValue(gas, out var value) ? value : null;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                return null;
            return seedData.ExudeGasses != null && seedData.ExudeGasses.TryGetValue(gas, out var value) ? value : null;
        }

        return GetDtoGeneValue(seedData, geneId);
    }

    private bool ApplyGeneToDto(SeedDataDto seedData, string geneId, object value, float? maxChemicalsVolume = null)
    {
        if (geneId.StartsWith(SeedDnaGeneEntry.ChemicalPrefix))
        {
            seedData.Chemicals ??= new Dictionary<string, SeedChemQuantityDto>();
            if (maxChemicalsVolume is { } maxVolume)
                AddChemical(seedData, geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..], (SeedChemQuantityDto)value, maxVolume);
            else
                seedData.Chemicals[geneId[SeedDnaGeneEntry.ChemicalPrefix.Length..]] = (SeedChemQuantityDto)value;
            return true;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ConsumeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ConsumeGasPrefix.Length..], out var gas))
                return false;
            seedData.ConsumeGasses ??= new Dictionary<Gas, float>();
            seedData.ConsumeGasses[gas] = (float)value;
            return true;
        }

        if (geneId.StartsWith(SeedDnaGeneEntry.ExudeGasPrefix))
        {
            if (!Enum.TryParse<Gas>(geneId[SeedDnaGeneEntry.ExudeGasPrefix.Length..], out var gas))
                return false;
            seedData.ExudeGasses ??= new Dictionary<Gas, float>();
            seedData.ExudeGasses[gas] = (float)value;
            return true;
        }

        SetDtoGeneValue(seedData, geneId, value);
        return true;
    }

    private static void AddChemical(SeedDataDto seedData, string reagentId, SeedChemQuantityDto quantity, float maxVolume)
    {
        var chemicals = seedData.Chemicals!;

        var currentVolume = chemicals.Values.Sum(chem => chem.Max);
        if (chemicals.TryGetValue(reagentId, out var existing))
            currentVolume -= existing.Max;

        while (currentVolume + quantity.Max > maxVolume)
        {
            var victim = chemicals
                .Where(pair => pair.Key != reagentId)
                .MinBy(pair => pair.Value.Max);

            if (victim.Key == null)
                break;

            currentVolume -= victim.Value.Max;
            chemicals.Remove(victim.Key);
        }

        chemicals[reagentId] = quantity;
    }

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
        var seedModified = component.SeedSlot.Item is { Valid: true } seedItem && IsSeedModified(seedItem);
        var stability = GetSeedStability(component.SeedSlot.Item);

        var newState = new SeedDnaConsoleBoundUserInterfaceState(
            seedPresent,
            seedName,
            diskPresent,
            diskName,
            BuildGeneEntries(uid, component, seedData, diskData),
            connected,
            seedData != null ? ComputeSellPrice(component, seedData) : 0,
            canSell,
            stability,
            seedData != null && (!component.RequireModifiedSeed || seedModified)
        );
        _userInterface.SetUiState(uid, SeedDnaConsoleUiKey.Key, newState);
    }

    private List<SeedDnaGeneEntry> BuildGeneEntries(
        EntityUid uid,
        SeedDnaConsoleComponent component,
        SeedDataDto? seedData,
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

            var rawSeed = seedData != null ? GetDtoGeneValue(seedData, geneId) : null;
            var rawDisk = diskData != null ? GetDtoGeneValue(diskData, geneId) : null;
            if (!HasMeaningfulValue(gene.ValueType, rawSeed) && !HasMeaningfulValue(gene.ValueType, rawDisk))
                continue;

            var lockedTech = GetLockedTech(uid, gene);

            entries.Add(new SeedDnaGeneEntry
            {
                Id = geneId,
                Name = $"seed-dna-row-{geneId}",
                Type = gene.ValueType,
                SeedValue = FormatGeneValue(gene.ValueType, () => seedData != null ? GetDtoGeneValue(seedData, geneId) : null),
                DiskValue = FormatGeneValue(gene.ValueType, () => diskData != null ? GetDtoGeneValue(diskData, geneId) : null),
                Locked = lockedTech != null,
                Cost = (int)gene.InstabilityCost,
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
        SeedDataDto? seedData,
        SeedDataDto? diskData)
    {
        var chemicals = new HashSet<string>();
        if (seedData?.Chemicals != null)
            chemicals.UnionWith(seedData.Chemicals.Keys);
        if (diskData?.Chemicals != null)
            chemicals.UnionWith(diskData.Chemicals.Keys);

        foreach (var reagentId in chemicals)
        {
            var seedPotency = seedData?.Potency;
            var diskPotency = diskData?.Potency ?? seedData?.Potency;

            SeedChemQuantityDto? seedChem = null;
            if (seedData?.Chemicals != null && seedData.Chemicals.TryGetValue(reagentId, out var seedQuantity))
                seedChem = seedQuantity;

            var lockedTech = GetLockedTech(uid, gene);

            entries.Add(new SeedDnaGeneEntry
            {
                Id = $"chemical:{reagentId}",
                Name = $"seed-dna-chemical-{reagentId}",
                Type = SeedDnaGeneType.Chemical,
                SeedValue = FormatChemical(seedChem, seedPotency),
                DiskValue = FormatChemical(diskData?.Chemicals?.GetValueOrDefault(reagentId), diskPotency),
                Locked = lockedTech != null,
                Cost = (int)component.InstabilityPerChemical,
                LockedTech = lockedTech,
            });
        }
    }

    private void AddGasEntries(
        List<SeedDnaGeneEntry> entries,
        EntityUid uid,
        SeedDnaConsoleComponent component,
        SeedDnaGenePrototype gene,
        SeedDataDto? seedData,
        SeedDataDto? diskData)
    {
        if ((seedData?.ConsumeGasses?.Count ?? 0) + (seedData?.ExudeGasses?.Count ?? 0)
            + (diskData?.ConsumeGasses?.Count ?? 0) + (diskData?.ExudeGasses?.Count ?? 0) == 0)
            return;

        var gasses = new HashSet<Gas>();
        if (seedData?.ConsumeGasses != null)
            gasses.UnionWith(seedData.ConsumeGasses.Keys);
        if (seedData?.ExudeGasses != null)
            gasses.UnionWith(seedData.ExudeGasses.Keys);
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
                SeedValue = FormatFloat(seedData?.ConsumeGasses?.GetValueOrDefault(gas)),
                DiskValue = FormatFloat(diskData?.ConsumeGasses?.GetValueOrDefault(gas)),
                Locked = lockedTech != null,
                Cost = (int)component.InstabilityPerGas,
                LockedTech = lockedTech,
            });

            entries.Add(new SeedDnaGeneEntry
            {
                Id = $"exudeGas:{gas}",
                Name = "seed-dna-row-exude-gas",
                GasName = gas.ToString(),
                Type = SeedDnaGeneType.Gas,
                SeedValue = FormatFloat(seedData?.ExudeGasses?.GetValueOrDefault(gas)),
                DiskValue = FormatFloat(diskData?.ExudeGasses?.GetValueOrDefault(gas)),
                Locked = lockedTech != null,
                Cost = (int)component.InstabilityPerGas,
                LockedTech = lockedTech,
            });
        }
    }

    private string? GetLockedTech(EntityUid uid, SeedDnaGenePrototype gene)
    {
        // Ген не привязан ни к одному исследованию — доступен с самого начала раунда.
        if (!TryGetGeneTechnology(gene.ID, out var technology))
            return null;

        if (_researchServer.TryGetClientServer(uid, out _, out _) && _research.IsTechnologyUnlocked(uid, technology))
            return null;

        return _prototypes.TryIndex<TechnologyPrototype>(technology, out var tech)
            ? Loc.GetString(tech.Name)
            : (string)technology;
    }

    private bool TryGetGeneTechnology(string geneId, out ProtoId<TechnologyPrototype> technology)
    {
        _geneTechnologies ??= BuildGeneTechnologies();
        return _geneTechnologies.TryGetValue(geneId, out technology);
    }

    private Dictionary<string, ProtoId<TechnologyPrototype>> BuildGeneTechnologies()
    {
        var map = new Dictionary<string, ProtoId<TechnologyPrototype>>();
        foreach (var tech in _prototypes.EnumeratePrototypes<TechnologyPrototype>())
        {
            foreach (var geneId in tech.UnlockedGenes)
            {
                if (!map.ContainsKey(geneId))
                    map[geneId] = tech.ID;
            }
        }

        return map;
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
                ? Loc.GetString($"seed-dna-harvest-{Enum.GetName((SharedHarvestTypeDto)harvest)}")
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

    private (bool, string, SeedDataDto?) ProcessSeedSlot(SeedDnaConsoleComponent component)
    {
        return component.SeedSlot.Item is not { Valid: true } seedItem
            ? (false, string.Empty, null)
            : (true, Name(seedItem), ExtractSeedData(seedItem));
    }

    private (bool, string, SeedDataDto?) ProcessDiskSlot(SeedDnaConsoleComponent component)
    {
        return component.DnaDiskSlot.Item is not { Valid: true } diskItem
            ? (false, string.Empty, null)
            : (true, Name(diskItem), Comp<DnaDiskComponent>(diskItem).SeedData);
    }

    #endregion
}
