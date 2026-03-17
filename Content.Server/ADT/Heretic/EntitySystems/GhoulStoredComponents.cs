using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Whitelist;

namespace Content.Server.Heretic.EntitySystems;

/// <summary>
///     Хранит данные компонентов для восстановления после смерти гуля.
/// </summary>
internal sealed class GhoulStoredComponents
{
    public RespiratorData? Respirator;
    public BarotraumaData? Barotrauma;
    public HungerData? Hunger;
    public ThirstData? Thirst;
    public ReproductiveData? Reproductive;
    public bool HasReproductivePartner;
    public TemperatureData? Temperature;
    public Color? StoredSkinColor;
    public Color? StoredEyeColor;
}

/// <summary>
///     Сохранённые данные RespiratorComponent.
/// </summary>
internal sealed class RespiratorData
{
    public float BreathVolume;
    public float Ratio;
    public TimeSpan NextUpdate;
    public TimeSpan UpdateInterval;
    public float UpdateIntervalMultiplier;
    public float Saturation;
    public float SuffocationThreshold;
    public float MaxSaturation;
    public float MinSaturation;
    public DamageSpecifier Damage = new();
    public DamageSpecifier DamageRecovery = new();
    public TimeSpan GaspEmoteCooldown;
    public TimeSpan LastGaspEmoteTime;
    public ProtoId<EmotePrototype>? GaspEmote;
    public int SuffocationCycles;
    public int SuffocationCycleThreshold;
    public RespiratorStatus Status;

    public RespiratorData()
    {
        Damage = new();
        DamageRecovery = new();
    }
}

/// <summary>
///     Сохранённые данные BarotraumaComponent.
/// </summary>
internal sealed class BarotraumaData
{
    public DamageSpecifier Damage = new();
    public FixedPoint2 MaxDamage;
    public List<string> ProtectionSlots = new();
    public bool TakingDamage;
    public float HighPressureMultiplier;
    public float HighPressureModifier;
    public float LowPressureMultiplier;
    public float LowPressureModifier;
    public bool HasImmunity;
    public ProtoId<AlertPrototype> HighPressureAlert;
    public ProtoId<AlertPrototype> LowPressureAlert;
    public ProtoId<AlertCategoryPrototype> PressureAlertCategory;

    public BarotraumaData()
    {
        Damage = new();
        ProtectionSlots = new();
    }
}

/// <summary>
///     Сохранённые данные HungerComponent.
/// </summary>
internal sealed class HungerData
{
    public float LastAuthoritativeHungerValue;
    public TimeSpan LastAuthoritativeHungerChangeTime;
    public float BaseDecayRate;
    public float ActualDecayRate;
    public HungerThreshold LastThreshold;
    public HungerThreshold CurrentThreshold;
    public Dictionary<HungerThreshold, float> Thresholds;
    public Dictionary<HungerThreshold, ProtoId<AlertPrototype>> HungerThresholdAlerts;
    public ProtoId<AlertCategoryPrototype> HungerAlertCategory;
    public Dictionary<HungerThreshold, float> HungerThresholdDecayModifiers;
    public float StarvingSlowdownModifier;
    public DamageSpecifier? StarvationDamage;
    public TimeSpan NextThresholdUpdateTime;
    public TimeSpan ThresholdUpdateRate;

    public HungerData()
    {
        Thresholds = new();
        HungerThresholdAlerts = new();
        HungerThresholdDecayModifiers = new();
    }
}

/// <summary>
///     Сохранённые данные ThirstComponent.
/// </summary>
internal sealed class ThirstData
{
    public float BaseDecayRate;
    public float ActualDecayRate;
    public ThirstThreshold CurrentThirstThreshold;
    public ThirstThreshold LastThirstThreshold;
    public float CurrentThirst;
    public TimeSpan NextUpdateTime;
    public TimeSpan UpdateRate;
    public Dictionary<ThirstThreshold, float> ThirstThresholds;
    public ProtoId<AlertCategoryPrototype> ThirstyCategory;

    public ThirstData()
    {
        ThirstThresholds = new();
    }
}

/// <summary>
///     Сохранённые данные ReproductiveComponent.
/// </summary>
internal sealed class ReproductiveData
{
    public TimeSpan NextBreedAttempt;
    public TimeSpan MinBreedAttemptInterval;
    public TimeSpan MaxBreedAttemptInterval;
    public float BreedRange;
    public int Capacity;
    public float BreedChance;
    public List<EntitySpawnEntry> Offspring;
    public bool Gestating;
    public TimeSpan? GestationEndTime;
    public TimeSpan GestationDuration;
    public float HungerPerBirth;
    public LocId BirthPopup;
    public bool MakeOffspringInfant;
    public EntityWhitelistData? PartnerWhitelist;

    public ReproductiveData()
    {
        Offspring = new();
    }
}

/// <summary>
///     Сохранённые данные EntityWhitelist.
/// </summary>
internal sealed class EntityWhitelistData
{
    public string[]? Components;
    public List<ProtoId<ItemSizePrototype>>? Sizes;
    public List<ProtoId<TagPrototype>>? Tags;
    public bool RequireAll;

    public EntityWhitelistData()
    {
        Sizes = new();
        Tags = new();
    }
}

/// <summary>
///     Сохранённые данные TemperatureComponent.
/// </summary>
internal sealed class TemperatureData
{
    public float CurrentTemperature;
    public float HeatDamageThreshold;
    public float ColdDamageThreshold;
    public float? ParentHeatDamageThreshold;
    public float? ParentColdDamageThreshold;
    public float SpecificHeat;
    public float AtmosTemperatureTransferEfficiency;
    public DamageSpecifier ColdDamage;
    public DamageSpecifier HeatDamage;
    public FixedPoint2 DamageCap;
    public bool TakingDamage;
    public ProtoId<AlertPrototype> HotAlert;
    public ProtoId<AlertPrototype> ColdAlert;

    public TemperatureData()
    {
        ColdDamage = new();
        HeatDamage = new();
    }
}
