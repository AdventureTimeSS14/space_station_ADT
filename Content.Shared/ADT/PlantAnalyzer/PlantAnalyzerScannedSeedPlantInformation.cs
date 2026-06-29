using Content.Shared.Atmos;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public NetEntity? TargetEntity;
    public bool IsTray;
    public bool IsMutating;

    public string? SeedName;
    public string[]? SeedChem;
    public AnalyzerHarvestType HarvestType;
    public Gas[] ExudeGases = [];
    public Gas[] ConsumeGases = [];
    public float Endurance;
    public int SeedYield;
    public float Lifespan;
    public float Maturation;
    public float Production;
    public int GrowthStages;
    public float SeedPotency;
    public string[]? Speciation;
    public AdvancedScanInfo AdvancedInfo;
}

/// <summary>
///     Information gathered during a scan (always included).
/// </summary>
[Serializable, NetSerializable]
public struct AdvancedScanInfo
{
    public float NutrientConsumption;
    public float WaterConsumption;
    public float IdealHeat;
    public float HeatTolerance;
    public float IdealLight;
    public float LightTolerance;
    public float ToxinsTolerance;
    public float LowPressureTolerance;
    public float HighPressureTolerance;
    public float PestTolerance;
    public float WeedTolerance;
    public MutationFlags Mutations;
    public bool Viable;
}

[Flags]
public enum MutationFlags : byte
{
    None = 0,
    TurnIntoKudzu = 1,
    Seedless = 2,
    Ligneous = 4,
    CanScream = 8,
}

public enum AnalyzerHarvestType : byte
{
    Unknown,
    Repeat,
    NoRepeat,
    SelfHarvest,
}
