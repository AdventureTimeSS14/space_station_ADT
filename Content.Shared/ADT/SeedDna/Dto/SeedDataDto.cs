using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos;
using Robust.Shared.Serialization;

// ReSharper disable once CheckNamespace
namespace Content.Shared.ADT.SeedDna;

/// <summary>
/// Контейнер для хранения свойств семян.
/// Нужен для передачи данных между UI клиента и сервером.
/// </summary>
[Serializable, NetSerializable]
public sealed class SeedDataDto
{
    public Dictionary<string, SeedChemQuantityDto>? Chemicals;
    public Dictionary<Gas, float>? ConsumeGasses;
    public Dictionary<Gas, float>? ExudeGasses;
    public float? NutrientConsumption;
    public float? WaterConsumption;
    public float? IdealHeat;
    public float? HeatTolerance;
    public float? ToxinsTolerance;
    public float? LowPressureTolerance;
    public float? HighPressureTolerance;
    public float? PestTolerance;
    public float? WeedTolerance;
    public float? Endurance;
    public int? Yield;
    public float? Lifespan;
    public float? Maturation;
    public float? Production;
    public SharedHarvestTypeDto? HarvestRepeat;
    public float? Potency;
    public bool? Seedless;
    public bool? Viable;
    public bool? Ligneous;
    public bool? CanScream;
}

[Serializable, NetSerializable]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum SharedHarvestTypeDto : byte
{
    NoRepeat,
    Repeat,
    SelfHarvest,
}
